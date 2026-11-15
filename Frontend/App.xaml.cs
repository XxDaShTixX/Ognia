using Ognia.Frontend.Helpers;
using System.IO;
using System.Windows;

namespace Ognia.Frontend;

public partial class App : Application
{
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

            // Theme must be in Application.Current.Resources BEFORE any Window parses
            // XAML that uses {StaticResource <theme-color-key>}.
            ThemeService.Initialize();

            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Warning;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(
                new System.Diagnostics.ConsoleTraceListener());

            // Manually create the window so it parses AFTER the theme is applied.
            new MainWindow().Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}