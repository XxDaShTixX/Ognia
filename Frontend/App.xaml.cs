using Ognia.Frontend.Helpers;
using System.IO;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace Ognia.Frontend;

public partial class App : Application
{
    private static UpdateManager? _updateManager;
    private static UpdateInfo? _pendingUpdate;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            AppPaths.EnsureAppDataFolder();

            if (string.IsNullOrWhiteSpace(AppSettings.Current.MusicRootFolder))
            {
                AppSettings.Current.MusicRootFolder = AppPaths.DefaultMusicRoot;
                AppSettings.Save();
            }

            Directory.CreateDirectory(AppSettings.Current.MusicRootFolder);

            ThemeService.Initialize();

            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Warning;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(
                new System.Diagnostics.ConsoleTraceListener());

            new MainWindow().Show();

            _ = CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var mgr = new UpdateManager(
                new GithubSource(
                    "https://github.com/XxDaShTixX/Ognia", 
                    accessToken: null, 
                    prerelease: true));

            if (!mgr.IsInstalled) return;

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null) return;

            await mgr.DownloadUpdatesAsync(newVersion);

            _updateManager = mgr;
            _pendingUpdate = newVersion;
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_updateManager != null && _pendingUpdate != null)
        {
            try { 
                _updateManager.WaitExitThenApplyUpdates(
                _pendingUpdate, 
                silent: true, 
                restart: false); 
            }
            catch { }
        }

        base.OnExit(e);
    }
}