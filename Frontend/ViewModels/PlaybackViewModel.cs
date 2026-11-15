using NAudio.Wave;
using Ognia.Frontend.Enums;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ognia.Frontend.ViewModels;

public class PlaybackViewModel : ViewModelBase, IDisposable
{
    private readonly WaveOut _waveOut = new();
    private AudioFileReader? _reader;
    private readonly DispatcherTimer _positionTimer;
    private readonly Random _random = new();

    private TimeSpan _currentPosition;
    private TimeSpan _totalDuration;
    private double _volume = 1.0;
    private bool _isMuted;
    private double _volumeBeforeMute = 1.0;
    private bool _isStarting;
    private bool _isSeeking;
    private bool _isAdvancing;
    private RepeatMode _repeatMode = RepeatMode.Off;
    private bool _isShuffleOn;
    private string _nowPlaying = string.Empty;
    private TrackRowViewModel? _currentTrack;
    private bool _disposed;


    public PlaybackViewModel()
    {
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionTimer.Tick += OnTimerTick;
        _positionTimer.Start();

        _waveOut.Volume = (float)_volume;

        PlayPauseCommand = new RelayCommand(_ => PlayPause(), _ => CurrentTrack != null);
        StopCommand = new RelayCommand(_ => Stop(), _ => PlaybackState != NAudio.Wave.PlaybackState.Stopped);
        NextCommand = new RelayCommand(async _ => await NextAsync(), _ => GetManualNextTrack() != null);
        PreviousCommand = new RelayCommand(async _ => await PreviousAsync(), _ => CanGoPrevious());
        ToggleMuteCommand = new RelayCommand(_ => ToggleMute());
        CycleRepeatCommand = new RelayCommand(_ => CycleRepeat());
        ToggleShuffleCommand = new RelayCommand(_ => IsShuffleOn = !IsShuffleOn);
    }

    #region Owner-supplied state

    /// <summary>The current playlist's tracks. Set by the owner when the playlist changes.</summary>
    public ObservableCollection<TrackRowViewModel> Tracks { get; set; } = new();

    /// <summary>
    /// Callback the owner wires to the download service.
    /// Returns true if the track is now local, false to abort playback.
    /// </summary>
    public Func<TrackRowViewModel, Task<bool>>? TrackDownloadRequested { get; set; }

    /// <summary>The track currently loaded into the player. Setting it refreshes command state.</summary>
    public TrackRowViewModel? CurrentTrack
    {
        get => _currentTrack;
        private set
        {
            if (_currentTrack == value) return;

            if (_currentTrack != null) _currentTrack.IsPlaying = false;
            _currentTrack = value;
            if (_currentTrack != null) _currentTrack.IsPlaying = true;

            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    #endregion

    #region Events

    /// <summary>Raised when a playback operation fails, with a user-facing message.</summary>
    public event Action<string>? ErrorOccurred;

    #endregion

    #region Commands

    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public RelayCommand CycleRepeatCommand { get; }
    public RelayCommand ToggleShuffleCommand { get; }

    #endregion

    #region Playback state

    public PlaybackState PlaybackState => _waveOut.PlaybackState;
    public bool IsPlaying => PlaybackState == PlaybackState.Playing;
    public bool IsPaused => PlaybackState == PlaybackState.Paused;
    public string PlayPauseLabel => IsPlaying ? "▐▐" : "▶";

    #endregion

    #region Position / duration

    public TimeSpan CurrentPosition
    {
        get => _currentPosition;
        set
        {
            if (!SetProperty(ref _currentPosition, value)) return;
            OnPropertyChanged(nameof(PositionLabel));

            // Apply immediately when changed outside an active drag (e.g. click-to-seek).
            if (!_isSeeking && _reader != null)
            {
                var clamped = value < TimeSpan.Zero ? TimeSpan.Zero
                    : value > _reader.TotalTime ? _reader.TotalTime
                    : value;
                try { _reader.CurrentTime = clamped; } catch { }
            }
        }
    }

    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set
        {
            if (SetProperty(ref _totalDuration, value))
                OnPropertyChanged(nameof(DurationLabel));
        }
    }

    public string PositionLabel => CurrentPosition.ToString(@"mm\:ss");
    public string DurationLabel => TotalDuration.ToString(@"mm\:ss");

    public string NowPlaying
    {
        get => _nowPlaying;
        set => SetProperty(ref _nowPlaying, value);
    }

    #endregion

    #region Volume / mute

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                _waveOut.Volume = (float)value;
                if (value > 0 && _isMuted)
                {
                    _isMuted = false;
                    OnPropertyChanged(nameof(IsMuted));
                    OnPropertyChanged(nameof(VolumeIcon));
                }
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetProperty(ref _isMuted, value))
                OnPropertyChanged(nameof(VolumeIcon));
        }
    }

    public string VolumeIcon => IsMuted ? "🔇" : "🔊";

    /// <summary>Mutes the player, remembering the previous volume, or restores it when unmuting.</summary>
    private void ToggleMute()
    {
        if (IsMuted)
        {
            IsMuted = false;
            Volume = _volumeBeforeMute;
        }
        else
        {
            _volumeBeforeMute = Volume > 0 ? Volume : 1.0;
            IsMuted = true;
            Volume = 0;
        }
    }

    #endregion

    #region Repeat / shuffle

    public RepeatMode RepeatMode
    {
        get => _repeatMode;
        set
        {
            if (SetProperty(ref _repeatMode, value))
            {
                OnPropertyChanged(nameof(RepeatLabel));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string RepeatLabel => RepeatMode switch
    {
        RepeatMode.Off => "➜",
        RepeatMode.All => "↳↰",
        RepeatMode.One => "⟲",
        _ => "➜"
    };

    public bool IsShuffleOn
    {
        get => _isShuffleOn;
        set
        {
            if (SetProperty(ref _isShuffleOn, value))
            {
                OnPropertyChanged(nameof(ShuffleLabel));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ShuffleLabel => IsShuffleOn ? "⇄" : "⊘";

    /// <summary>Cycles the repeat mode Off → All → One → Off.</summary>
    private void CycleRepeat()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.Off,
            _ => RepeatMode.Off
        };
    }

    #endregion

    #region Transport: play / pause / stop

    /// <summary>
    /// Toggles pause/resume if a track is loaded, otherwise starts the current track
    /// (downloading it first if needed). Guards against double-start while preparing.
    /// </summary>
    public void PlayPause()
    {
        switch (_waveOut.PlaybackState)
        {
            case NAudio.Wave.PlaybackState.Playing:
                _waveOut.Pause();
                NotifyPlaybackChanged();
                return;

            case NAudio.Wave.PlaybackState.Paused:
                _waveOut.Play();
                NotifyPlaybackChanged();
                return;
        }

        if (CurrentTrack == null || _isStarting) return;

        _isStarting = true;
        _ = StartPlayAsync(CurrentTrack);
    }

    /// <summary>Runs PlayTrackAsync while the _isStarting guard is held, then releases it.</summary>
    private async Task StartPlayAsync(TrackRowViewModel track)
    {
        try { await PlayTrackAsync(track); }
        finally { _isStarting = false; }
    }

    /// <summary>Stops playback, disposes the reader, and resets position, duration, and now-playing.</summary>
    public void Stop()
    {
        _waveOut.Stop();
        _reader?.Dispose();
        _reader = null;
        CurrentPosition = TimeSpan.Zero;
        TotalDuration = TimeSpan.Zero;
        NowPlaying = string.Empty;
        NotifyPlaybackChanged();
    }

    /// <summary>
    /// Plays a specific track: downloads it first if not local, then starts playback.
    /// Aborts via Stop() if the download fails or no download callback is wired.
    /// </summary>
    public async Task PlayTrackAsync(TrackRowViewModel track)
    {
        CurrentTrack = track;

        if (!track.IsDownloaded)
        {
            NowPlaying = $"Preparing '{track.Name}'…";
            if (TrackDownloadRequested == null)
            {
                Stop();
                return;
            }

            var ok = await TrackDownloadRequested(track);
            if (!ok)
            {
                Stop();
                return;
            }
        }

        StartCurrentTrack();
    }

    /// <summary>
    /// Loads the current track's local file into NAudio and starts playback.
    /// Stops if the file cannot be found or the reader fails to initialize.
    /// </summary>
    private void StartCurrentTrack()
    {
        if (CurrentTrack == null) return;

        var path = CurrentTrack.LocalFilePath;
        if (path == null)
        {
            ErrorOccurred?.Invoke($"No local file for '{CurrentTrack.Name}'.");
            Stop();
            return;
        }

        try
        {
            _waveOut.Stop();
            _reader?.Dispose();
            _reader = new AudioFileReader(path);
            _waveOut.Init(_reader);
            TotalDuration = _reader.TotalTime;
            CurrentPosition = TimeSpan.Zero;
            _waveOut.Play();
            NowPlaying = CurrentTrack.Name;
            NotifyPlaybackChanged();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Playback failed: {ex.Message}");
            Stop();
        }
    }

    #endregion

    #region Transport: next / previous

    /// <summary>Plays the next track according to shuffle/repeat rules (manual skip ignores Repeat One).</summary>
    private async Task NextAsync()
    {
        var next = GetManualNextTrack();
        if (next == null) return;
        await PlayTrackAsync(next);
    }

    /// <summary>Plays the previous track, wrapping to the last one when Repeat All is on.</summary>
    private async Task PreviousAsync()
    {
        if (CurrentTrack == null) return;

        var index = Tracks.IndexOf(CurrentTrack);
        if (index <= 0)
        {
            if (RepeatMode == RepeatMode.All && Tracks.Count > 0)
                await PlayTrackAsync(Tracks[^1]);
            return;
        }

        await PlayTrackAsync(Tracks[index - 1]);
    }

    /// <summary>Whether a previous track can be reached (sequentially, or by wrapping with Repeat All).</summary>
    private bool CanGoPrevious()
    {
        if (CurrentTrack == null) return false;
        var index = Tracks.IndexOf(CurrentTrack);
        if (index < 0) return false;
        return index > 0 || (RepeatMode == RepeatMode.All && Tracks.Count > 0);
    }

    /// <summary>Resolves the track auto-advance should play, honoring Repeat One.</summary>
    private TrackRowViewModel? GetAutoAdvanceTrack()
    {
        if (CurrentTrack == null || Tracks.Count == 0) return null;
        if (RepeatMode == RepeatMode.One) return CurrentTrack;
        return GetSequentialOrShuffleNext();
    }

    /// <summary>Resolves the track a manual Next should play, ignoring Repeat One.</summary>
    private TrackRowViewModel? GetManualNextTrack()
    {
        if (CurrentTrack == null || Tracks.Count == 0) return null;
        return GetSequentialOrShuffleNext();
    }

    /// <summary>
    /// Picks the next track: random (excluding current) when shuffle is on,
    /// otherwise the following track, wrapping to the first only with Repeat All.
    /// </summary>
    private TrackRowViewModel? GetSequentialOrShuffleNext()
    {
        if (CurrentTrack == null) return null;

        if (IsShuffleOn && Tracks.Count > 1)
        {
            var candidates = Tracks.Where(t => t != CurrentTrack).ToList();
            return candidates[_random.Next(candidates.Count)];
        }

        var index = Tracks.IndexOf(CurrentTrack);
        if (index < 0) return null;

        var nextIndex = index + 1;
        if (nextIndex >= Tracks.Count)
            return RepeatMode == RepeatMode.All ? Tracks[0] : null;

        return Tracks[nextIndex];
    }

    /// <summary>
    /// Re-resolves CurrentTrack to the matching instance in Tracks (matched by Id).
    /// If the track is not in the new collection, leaves CurrentTrack untouched —
    /// playback continues against the existing instance and the playback bar keeps rendering it.
    /// </summary>
    public void RebindCurrentTrack()
    {
        if (_currentTrack == null) return;

        var id = _currentTrack.Id;
        var match = Tracks.FirstOrDefault(t => t.Id == id);
        if (match == null) return;

        if (ReferenceEquals(match, _currentTrack)) return;

        _currentTrack.IsPlaying = false;
        match.IsPlaying = true;
        _currentTrack = match;
        OnPropertyChanged(nameof(CurrentTrack));
        CommandManager.InvalidateRequerySuggested();
    }

    #endregion

    #region Seeking

    /// <summary>Pauses the position timer while the user drags the seek slider.</summary>
    public void BeginSeek()
    {
        _isSeeking = true;
        _positionTimer.Stop();
    }

    /// <summary>Applies the sought position to the reader, then restarts the timer.</summary>
    public void EndSeek(TimeSpan position)
    {
        if (_reader != null)
        {
            _reader.CurrentTime = position;
            CurrentPosition = position;
        }

        _isSeeking = false;
        _positionTimer.Start();
    }

    #endregion

    #region Timer

    /// <summary>
    /// Polls playback position every 500ms; on track end, advances to the next track
    /// (honoring repeat/shuffle) or stops when the playlist is exhausted.
    /// </summary>
    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isSeeking || _isAdvancing) return;

        if (_reader != null && _waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            CurrentPosition = _reader.CurrentTime;

        if (TotalDuration > TimeSpan.Zero && CurrentPosition >= TotalDuration - TimeSpan.FromSeconds(0.5))
        {
            CurrentPosition = TimeSpan.Zero;
            TotalDuration = TimeSpan.Zero;

            var next = GetAutoAdvanceTrack();
            if (next != null)
            {
                _isAdvancing = true;
                try { await PlayTrackAsync(next); }
                finally { _isAdvancing = false; }
            }
            else
            {
                Stop();
            }
        }
    }

    #endregion

    #region Notifications

    /// <summary>Raises change notifications for all playback state properties and re-queries commands.</summary>
    private void NotifyPlaybackChanged()
    {
        OnPropertyChanged(nameof(PlaybackState));
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PlayPauseLabel));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _positionTimer.Stop();
        _positionTimer.Tick -= OnTimerTick;

        try { _waveOut.Stop(); } catch { }

        _reader?.Dispose();
        _reader = null;

        _waveOut.Dispose();
    }

    #endregion
}