using Ognia.Frontend.Helpers;
using Ognia.Frontend.Services;
using System.Windows;

namespace Ognia.Frontend;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            AppPaths.Initialize();
            ThemeService.Initialize();

            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level =
                System.Diagnostics.SourceLevels.Warning;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(
                new System.Diagnostics.ConsoleTraceListener());

            new MainWindow().Show();

            if (AppSettings.Instance.Update.AutoUpdateEnabled)
                _ = UpdateService.Instance.CheckAsync(downloadIfFound: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (AppSettings.Instance.Update.AutoUpdateEnabled)
            UpdateService.Instance.ApplyOnExit();

        base.OnExit(e);
    }
}