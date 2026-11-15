# Ognia

A desktop music player that turns YouTube links into a local, offline-capable music library. Built as a learning project: an ASP.NET Core backend for accounts, playlists, and track metadata, and a WPF client that downloads audio, plays it locally, and works like a small Spotify.

## Table of contents

- [Features](#features)
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
- [How storage works](#how-storage-works)
- [Track sources](#track-sources)
- [Adding Spotify (and other sources)](#adding-spotify-and-other-sources)
- [Implementation guide](#implementation-guide)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Legal](#legal)

## Features

**Accounts**
- Email/password registration and login (ASP.NET Core Identity + JWT)
- Persisted sessions across app restarts, with expiry handling
- Forgot-password / reset-password flow (dev mode: token printed to the backend console)
- Change password while logged in
- Automatic logout and redirect to login when a token becomes invalid (401)

**Playlists**
- Create, rename, delete playlists
- Selection preserved across reloads
- Track counts shown per playlist

**Tracks**
- Add a track by pasting a YouTube URL
- Metadata (title, duration) fetched server-side
- Rename tracks (applies everywhere the track appears)
- Remove from a playlist
- Tracks are shared across playlists — one row, many memberships

**Downloads and playback**
- Download audio to a single local folder
- Download all undownloaded tracks in a playlist, with progress
- Play/pause, stop, next, previous
- Position slider with seeking
- Volume slider and mute toggle
- "Now playing" indicator
- Repeat (Off / All / One) and shuffle
- Auto-advance on track end, downloading the next track on demand

## Architecture

```text
+---------------------+         HTTPS/JSON        +----------------------+
|   WPF Desktop App   |  ----------------------->  |  ASP.NET Core API    |
|     (Frontend)      |  <-----------------------  |      (Backend)       |
+---------------------+      JWT in header        +----------------------+
         |                                                   |
         v                                                   v
+---------------------+                            +----------------------+
|  Local audio files  |                            |   SQLite database    |
|   + settings.json   |                            |  users, playlists,   |
|   + token.json      |                            |  tracks, links       |
+---------------------+                            +----------------------+
```

The backend is the source of truth for users, playlists, and track metadata. The client is the source of truth for local audio files and user preferences.

**Design principle:** the client downloads audio directly from YouTube (via `YoutubeExplode`); the backend never handles audio bytes. The backend's only source-related job is fetching metadata (title, duration) when a track is added.

## Tech stack

**Backend**
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQLite
- ASP.NET Core Identity (JWT bearer tokens)
- Swashbuckle (Swagger UI)
- `YoutubeExplode` (metadata)

**Frontend**
- .NET 10 / WPF
- MVVM (no framework — plain `INotifyPropertyChanged`, `ObservableCollection`, `ICommand`)
- `NAudio` (playback)
- `YoutubeExplode` (audio download)
- `System.Text.Json` (settings and token persistence)

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows (the WPF client is Windows-only)
- A modern IDE: Visual Studio 2022+, Rider, or VS Code with the C# extension

### Backend

1. Navigate to the backend project.

```bash
cd Backend
```

2. Apply migrations and create the SQLite database.

```bash
dotnet ef database update
```

3. Configure a JWT signing key in `appsettings.json`. Replace the placeholder `Jwt:Key` with a random string of at least 32 characters.

```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS",
    "Issuer": "Ognia",
    "Audience": "OgniaUsers",
    "ExpiryMinutes": 10080
  }
}
```

`ExpiryMinutes: 10080` is 7 days. For production, use a shorter value and a refresh-token flow.

4. Run the backend.

```bash
dotnet run
```
Swagger opens at `https://localhost:<port>/swagger`.

### Frontend

1. Trust the ASP.NET development certificate (one-time).

```bash
dotnet dev-certs https --trust
```

2. Set the backend base URL in `Frontend/Helpers/ApiConfig.cs`.

```csharp
public const string BaseUrl = "https://localhost:7235/";
```

Match the port the backend prints on startup.

3. Run the WPF project from your IDE, or:

```bash
cd Frontend
dotnet run
```

### First use

1. Register an account from the login view.
2. Create a playlist.
3. Paste a YouTube URL into the "Add track" box.
4. Click **Download** to fetch the audio locally.
5. Click **Play**.

## How storage works

### Local files

Audio is stored in a single folder:

```text
%USERPROFILE%\Music\Ognia\
  42 - Thunderstruck.m4a
  43 - Bohemian Rhapsody.m4a
  ...
```

**Filename convention:** `{trackId} - {sanitized track name}.m4a`. The numeric prefix is the track's database ID and is how the app finds the file. The name portion is cosmetic — it can be stale (renaming a track doesn't rename the file) and the app still finds it by the ID prefix.

**One file per track.** Playlists are organizational: the same track can appear in many playlists, but only one copy exists on disk. Deleting a playlist doesn't delete files directly — it removes the playlist's references, and a file is deleted only when no playlist references its track anymore.

### App data

Stored in `%LOCALAPPDATA%\Ognia\`:

| File | Purpose |
|---|---|
| `settings.json` | The music root folder path |
| `token.json` | The JWT and its expiry (deleted on logout or expiry) |

If you need to force a logout, delete `token.json`. If you need to reset the app's settings, delete `settings.json`.

### Database

The backend uses SQLite at `Backend/musify.db`. Tables:

- `AspNetUsers`, `AspNetRoles`, etc. — Identity's standard tables
- `Tracks` — one row per unique `(SourceName, SourceId)`, with name and duration
- `Playlists` — owned by a user
- `PlaylistTracks` — the many-to-many join with `Position` for ordering

The `Tracks` table is shared across users: two users who add the same YouTube video reference the same row. A `Track` row is deleted only when no playlist (by any user) references it.

## Track sources

Each track carries a `(SourceName, SourceId)` pair. `SourceName` identifies the platform (`"youtube"`), and `SourceId` is the platform-specific identifier (an 11-character YouTube video ID).

URLs are normalized to IDs on add, so all of these resolve to the same track:

- `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
- `https://youtu.be/dQw4w9WgXcQ`
- `https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=42s`
- `https://music.youtube.com/watch?v=dQw4w9WgXcQ`

### Adding a new source

The `ITrackSourceService` interface defines what a source must implement:

```csharp
public interface ITrackSourceService
{
    TrackSource SourceType { get; }
    string SourceName => SourceType.ToWireName();
    bool CanHandle(string url);
    string? ExtractId(string url);
    Task<TrackMetadataModel> FetchMetadataAsync(string sourceId, CancellationToken ct = default);
}
```

To add a source:

1. Add a member to the `TrackSource` enum.
2. Add its wire name to `TrackSourceExtensions.ToWireName` / `FromWireName`.
3. Implement `ITrackSourceService` for the new source.
4. Register it in `Program.cs` alongside the YouTube implementation.
5. If the client also needs to download from it, add a corresponding downloader in the frontend.

The add-track endpoint already picks the right `ITrackSourceService` by calling `CanHandle` on each registered implementation.

## Adding Spotify (and other sources)

**The important constraint: Spotify is metadata-only.** Spotify's Developer Terms prohibit downloading or saving Spotify content, and the Web API does not expose full audio streams (only 30-second preview clips, with restrictions). So Spotify can be added as a metadata source — a way to catalog and reference tracks — but not as a download source.

What that means:
- **Can build:** paste a Spotify track URL, backend fetches name, artist, duration, track appears in playlists.
- **Can't build:** download the audio, play it offline, or play it through the existing NAudio path.
- **To play Spotify tracks** you would need a separate feature: embedding the Web Playback SDK in a WebView, which requires Spotify Premium, OAuth, and a different playback architecture.

What changes:

1. The enum:

```csharp
public enum TrackSource
{
    YouTube,
    Spotify
}
```

Add `"spotify"` to `ToWireName` / `FromWireName`.

2. `SpotifyTrackSourceService` implementing `ITrackSourceService`:
   - `SourceType` returns `TrackSource.Spotify`
   - `CanHandle` matches `open.spotify.com/track/` and `spotify:track:`
   - `ExtractId` parses the 22-character Base62 track ID
   - `FetchMetadataAsync` calls `GET https://api.spotify.com/v1/tracks/{id}` with a bearer token

   Use SpotifyAPI-NET rather than hand-rolling HTTP. It handles Client Credentials, token refresh, and rate limiting.

3. Configuration in `appsettings.json`:

```json
"Spotify": {
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret"
}
```

Register an app at https://developer.spotify.com/dashboard to get these. Client Credentials doesn't require a user login.

4. Registration in `Program.cs`:

```csharp
builder.Services.AddSingleton<ITrackSourceService, YouTubeTrackSourceService>();
builder.Services.AddSingleton<ITrackSourceService, SpotifyTrackSourceService>();
```

5. **The client guard:** when a track's source can't be downloaded, disable Play and Download and explain why. Expose a `CanPlay` / `CanDownload` flag on `TrackRowViewModel` derived from the source, bind the Play/Download `CanExecute` to it, and show a tooltip like "Spotify tracks cannot be downloaded."

## Implementation guide

### Backend: authentication flow

1. `POST /api/auth/register` creates an `AppUser` via `UserManager.CreateAsync`.
2. `POST /api/auth/login` verifies the password with `SignInManager.CheckPasswordSignInAsync`, then issues a JWT signed with `Jwt:Key`.
3. The JWT carries `sub` (user ID) and `email` claims. The client stores it in `%LOCALAPPDATA%\Ognia\token.json`.
4. Every authenticated request sends `Authorization: Bearer <token>`.
5. The JWT middleware validates the token on each request. Claims are read via `ClaimTypes.NameIdentifier` and `ClaimTypes.Email`.
6. On a `401`, `ApiClient` raises its `Unauthorized` event; `MainWindow` clears the token and navigates to login.

Password reset uses `GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`. In development, the reset token is printed to the backend console instead of being emailed. Change-password uses `ChangePasswordAsync` and requires the current password.

### Backend: adding a track

`POST /api/playlists/{id}/tracks` takes `{ "url": "..." }`. The flow:

1. Verify the playlist belongs to the calling user.
2. Iterate registered `ITrackSourceService` implementations; pick the first where `CanHandle(url)` is true.
3. Call `ExtractId(url)` to get the source-specific ID. Reject if null.
4. Look up an existing `Track` by `(SourceName, SourceId)`. If found, reuse it.
5. If not found, call `FetchMetadataAsync(sourceId)`, create the `Track`, save.
6. Reject if the track is already in this playlist (`409 Conflict`).
7. Append a `PlaylistTrack` row with `Position = max + 1`.
8. Return a `TrackResponse`.

Track reuse means adding the same YouTube video to two playlists creates one `Track` row and two `PlaylistTrack` rows.

### Backend: removing a track and orphan cleanup

`DELETE /api/playlists/{playlistId}/tracks/{trackId}` removes the `PlaylistTrack` row, then calls `DeleteTrackIfOrphanedAsync`:

```csharp
private async Task DeleteTrackIfOrphanedAsync(int trackId)
{
    var stillReferenced = await _db.PlaylistTracks.AnyAsync(pt => pt.TrackId == trackId);
    if (stillReferenced) return;

    var track = await _db.Tracks.FindAsync(trackId);
    if (track != null)
    {
        _db.Tracks.Remove(track);
        await _db.SaveChangesAsync();
    }
}
```

The reference check is **global** — any playlist by any user. The `Tracks` table is shared, so a row is deleted only when nobody references it.

`DELETE /api/playlists/{id}` captures the track IDs before the cascade removes the links, then runs the same orphan check per track.

### Backend: usage endpoint

`GET /api/tracks/{id}/usage` returns `{ "playlistCount": N }`, counting **only the current user's playlists**. This is deliberately different from the orphan cleanup's global check:

- **Usage endpoint** answers "can I delete my local file?" — user-scoped, because your file is yours.
- **Orphan cleanup** answers "can I delete the shared row?" — global, because the row is shared.

Both are correct for their respective questions.

### Frontend: startup and navigation

1. `App.xaml` starts the application. `OnStartup` ensures `%LOCALAPPDATA%\Ognia\` exists, writes default settings if missing, and creates the music root folder.
2. `MainWindow` constructs the shared `ApiClient`, subscribes to its `Unauthorized` event, then checks `TokenStore.IsAuthenticated`:
   - Valid token → `ShowMain()`
   - No valid token → `ShowLogin()`
3. `ShowMain`, `ShowLogin`, `ShowAccount`, `ShowResetPassword` swap `RootContent.Content` to the corresponding `UserControl`.
4. Each view receives the shared `ApiClient` via its constructor, which avoids the `Window.GetWindow(this)` null-cast at construction time.

### Frontend: MVVM structure

Plain MVVM, no framework:

- `ViewModelBase` implements `INotifyPropertyChanged` and provides `SetProperty`.
- `RelayCommand` implements `ICommand`, with `CanExecuteChanged` wired to `CommandManager.RequerySuggested`.
- Views set their `DataContext` to a view model in the constructor and bind to properties and commands.
- Code-behind is used only for view lifecycle and for things that genuinely can't be bound, like `PasswordBox.PasswordChanged`.

### Frontend: MainViewModel

The coordinator. It holds:

- `Playlists` and `SelectedPlaylistTracks` collections
- `SelectedPlaylist` and `SelectedTrack`
- Input fields (`NewPlaylistName`, `RenameTrackName`, `NewTrackUrl`, `RenamePlaylistName`)
- `IsLoading`, `ErrorMessage`, `WelcomeText`
- `Playback` and `Download` service instances
- Playlist and track CRUD commands

It forwards state to the services:

- `SelectedPlaylist` → triggers `LoadSelectedPlaylistTracksAsync`
- `SelectedTrack` → sets `Playback.CurrentTrack` and `Download.SelectedTrack`
- Track list reload → sets `Playback.Tracks` and `Download.Tracks`

And it wires the cross-service dependency:

```csharp
Playback.TrackDownloadRequested = track => Download.EnsureDownloadedAsync(track);
Playback.ErrorOccurred += msg => ErrorMessage = msg;
Download.ErrorOccurred += msg => ErrorMessage = msg;
```

### Frontend: PlaybackService

Owns NAudio and all transport state. Key points:

- `WaveOut` + `AudioFileReader` for playback. `DispatcherTimer` polls position every 500ms.
- `PlaybackState` is a pass-through to `WaveOut.PlaybackState`; `IsPlaying`, `IsPaused`, `PlayPauseLabel` derive from it. `NotifyPlaybackChanged()` raises change notifications after each transport action.
- Next/previous resolve through `GetSequentialOrShuffleNext`, which honors shuffle (random different track) and repeat (wrap to first with Repeat All, stop with Repeat Off).
- Auto-advance uses `GetAutoAdvanceTrack`, which returns the current track under Repeat One.
- On track end, the timer checks `CurrentPosition >= TotalDuration - 0.5s`, clears position/duration to prevent re-firing, then advances or stops.
- `_isStarting` guards against double-start while a download is in flight; `_isAdvancing` guards against re-entrant auto-advance; `_isSeeking` pauses the timer while the user drags the slider.
- `TrackDownloadRequested` is the callback to the download service — playback doesn't import the downloader.
- `ErrorOccurred` surfaces playback failures to `MainViewModel`, which sets `ErrorMessage`.

### Frontend: DownloadService

Owns file downloading and progress state.

- `SelectedTrack` and `Tracks` are owner-supplied.
- `DownloadTrackCommand`, `RemoveDownloadCommand`, `DownloadAllCommand` operate on those.
- `DownloadSelectedAsync` downloads one track; `DownloadAllAsync` iterates undownloaded tracks sequentially, reporting `DownloadAllCompleted / DownloadAllTotal`.
- `EnsureDownloadedAsync(track)` is what `PlaybackService` calls via its callback. Returns `true` if the track is now local, `false` on failure.
- Failures raise `ErrorOccurred`; the batch operation counts failures and reports them at the end rather than aborting.
- `TrackDownloaderService` does the actual `YoutubeExplode` call and file write.

### Frontend: local file lookup

`LocalLibrary` is the filesystem layer:

- `FindLocalFile(trackId)` scans the music root for a file whose leading numeric prefix matches. Returns the full path or null.
- `GetDownloadedTrackIds()` does one pass and returns all IDs, for decorating a playlist view.
- `DeleteLocalFile(trackId)` finds and deletes, returns whether anything was deleted.
- `BuildFileName(trackId, trackName)` produces `{id} - {sanitized name}.m4a`.
- `ParseTrackId(filePath)` extracts the leading ID from a filename.

The filename convention is the contract: the ID prefix is how files are found, the name is cosmetic.

### Frontend: the usage provider seam

`ITrackUsageProvider` answers "how many playlists reference this track?":

```csharp
public interface ITrackUsageProvider
{
    Task<int> GetPlaylistCountAsync(int trackId);
}
```

`ApiTrackUsageProvider` calls `GET /api/tracks/{id}/usage` and returns the count, or `-1` on any failure. The `-1` sentinel is the safety contract: `MainViewModel.DeleteLocalFileIfUnusedAsync` treats `-1` as "unknown, keep the file" and `0` as "safe to delete."

This is the piece guest mode will plug into: a `LocalTrackUsageProvider` answering the same question from local storage, with no changes to `MainViewModel`.

### Frontend: delete flows

**Remove track from playlist:**

1. Call the remove endpoint.
2. Reload tracks and playlists (so the usage count excludes this playlist).
3. `DeleteLocalFileIfUnusedAsync(trackId, name, warnings)` — checks usage, deletes the file if 0, records a warning if -1.
4. Report warnings after the reload (which clears `ErrorMessage`).

**Delete playlist:**

1. Snapshot track IDs and names from the current track list.
2. Call the delete endpoint.
3. Reload playlists.
4. Run `DeleteLocalFileIfUnusedAsync` for each snapshotted track.
5. Report warnings.

Both flows rely on the backend having already removed the link, so the usage count reflects the post-delete state.

## Troubleshooting

**App launches but no window appears.**
`App.xaml` is missing `StartupUri="MainWindow.xaml"`, and `OnStartup` doesn't open a window. Add `StartupUri`, or call `new MainWindow().Show()` at the end of `OnStartup` — not both.

**"The name 'X' does not exist in the namespace 'clr-namespace:...'" after adding a converter or resource.**
XAML code generation is stale. Clean Solution, then Rebuild Solution. If it persists, check the namespace in the `.cs` file matches the `clr-namespace:` string exactly, including casing.

**Download buttons silently do nothing.**
The command binding path is wrong. WPF doesn't error on a missing binding — it disables the button. Check that `DownloadTrackCommand` is bound as `Download.DownloadTrackCommand` (it lives on `DownloadService`, not `MainViewModel`).

**Play button does nothing on an undownloaded track.**
`PlayPause` routes through `PlayTrackAsync`, which calls `TrackDownloadRequested`. If that callback isn't wired, or the download fails silently, playback aborts. Check that `Playback.TrackDownloadRequested` is assigned in the `MainViewModel` constructor.

**SSL error connecting to the backend from the WPF app.**
The dev certificate isn't trusted. Run `dotnet dev-certs https --trust`.

**Token expires and the app shows errors instead of returning to login.**
Check that `ApiClient` calls `RaiseUnauthorizedIfNeeded(response)` in every authenticated method, and that `MainWindow` subscribes to `Api.Unauthorized`.

**Files download but the checkmark indicator doesn't appear.**
`TrackRowViewModel.IsDownloaded` is set at construction by scanning the filesystem. After a download, `DownloadService` sets `SelectedTrack.IsDownloaded = true` directly. If the checkmark doesn't appear, the row instance may have been replaced by a reload.

**A deleted playlist's files remain on disk.**
Expected for shared tracks — a file is deleted only when no playlist references its track. Check the usage endpoint returns 0 for the tracks you expected to be deleted.

## Contributing

This is a personal learning project, but the structure is intentional. If you extend it:

- **New source**: implement `ITrackSourceService`, add the enum member and wire names, register it. If it supports downloads, add a client-side downloader and a Play/Download guard for sources that don't.
- **New feature**: decide where it belongs. NAudio-related → `PlaybackService`. Filesystem/download-related → `DownloadService`. Orchestration → `MainViewModel`. Anything that answers a question and might be backed by local storage later → a provider under `Providers/`.
- **New endpoint**: use a named DTO for the request and response. No anonymous objects.
- **New client API call**: add it to `ApiClient` with the `(bool Success, string? Error, T? Data)` tuple shape, and call `RaiseUnauthorizedIfNeeded` if it's authenticated.

## Legal

This is an educational project. It does **not** host, distribute, or own any copyrighted audio. Audio is downloaded by the client from YouTube, subject to YouTube's terms of service and applicable copyright law. The `YoutubeExplode` library is used for interoperability.

If you distribute a build of this app, be aware that:

- YouTube's terms prohibit downloading content for most uses.
- App stores (Apple, Google) generally reject apps with built-in YouTube-downloading functionality.
- Distributing the app on GitHub or as a direct download sidesteps store review but doesn't change the underlying legal risk.

Use it for personal learning and experimentation. Don't redistribute content you don't have rights to.
