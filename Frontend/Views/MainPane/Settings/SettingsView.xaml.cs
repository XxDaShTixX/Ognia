using System.Windows;
using System.Windows.Controls;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Services;
using Ognia.Frontend.Session;
using Ognia.Frontend.ViewModels;

namespace Ognia.Frontend.Views.MainPane.Settings;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsView(ApiClientService api, IAppNavigator nav, AppSession session)
    {
        InitializeComponent();
        _vm = new SettingsViewModel(api, nav, session);
        DataContext = _vm;
    }

    private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.CurrentPassword = ((PasswordBox)sender).Password;

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.NewPassword = ((PasswordBox)sender).Password;

    private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.ConfirmPassword = ((PasswordBox)sender).Password;

    private void ExportLibrary_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export library",
            Filter = "Ognia library (*.json)|*.json",
            FileName = $"ognia-library-{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() != true) return;

        var (success, error) = _vm.ExportLibrary(dialog.FileName);
        if (success) _vm.Session.SetStatus("Library exported.");
        else _vm.Session.SetError(error ?? "Export failed.");
    }

    private async void ImportLibrary_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import library",
            Filter = "Ognia library (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true) return;

        var (success, error, count) = _vm.ImportLibrary(dialog.FileName);

        if (success)
        {
            _vm.Session.SetStatus($"Imported {count} playlist(s).");
            await _vm.Session.LoadPlaylistsAsync();
        }
        else
        {
            _vm.Session.SetError(error ?? "Import failed.");
        }
    }

    private async void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        var (success, error, tempPath) = await _vm.PrepareUpgradeAsync();
        if (!success)
        {
            _vm.Session.SetError(error ?? "Upgrade failed.");
            return;
        }

        MessageBox.Show(
            $"Your library has been backed up to:\n{tempPath}\n\n" +
            "You'll be taken to the login screen. Register or log in, " +
            "and your library will be uploaded automatically.",
            "Upgrade to Account",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        _vm.BeginUpgrade(tempPath!);
    }
}