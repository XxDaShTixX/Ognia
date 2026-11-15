using Ognia.Frontend.Enums;
using Ognia.Frontend.Helpers;
using Ognia.Frontend.Models.Auth;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Repos;
using Ognia.Frontend.Services;
using Ognia.Frontend.Session;

namespace Ognia.Frontend.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ApiClientService _api;
    private readonly IAppNavigator _nav;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isError;

    public SettingsViewModel(ApiClientService api, IAppNavigator nav, AppSession session)
    {
        _api = api;
        _nav = nav;
        Session = session;

        ChangePasswordCommand = new RelayCommand(
            async _ => await ChangePasswordAsync(),
            _ => !string.IsNullOrWhiteSpace(CurrentPassword)
                 && !string.IsNullOrWhiteSpace(NewPassword)
                 && NewPassword == ConfirmPassword);

        LogoutCommand = new RelayCommand(async _ => await LogoutAsync());

        AvailableThemes = ThemeService.EnumerateThemes();

        var applied = ThemeService.ActiveThemeFileName
                      ?? AppSettings.Current.ActiveTheme
                      ?? "Default.json";

        _selectedTheme = AvailableThemes
            .FirstOrDefault(t => string.Equals(t.FileName, applied, StringComparison.OrdinalIgnoreCase))
            ?? AvailableThemes.FirstOrDefault();
    }

    public AppSession Session { get; }

    public bool IsGuestMode => AppSettings.Current.Mode == AppMode.Guest;

    public IReadOnlyList<ThemeInfo> AvailableThemes { get; }

    private ThemeInfo? _selectedTheme;
    public ThemeInfo? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value)) return;
            if (value == null) return;

            if (ThemeService.Apply(value.FileName))
            {
                AppSettings.Current.ActiveTheme = value.FileName;
                AppSettings.Save();
                Session.SetStatus($"Applied theme '{value.Name}'.");
            }
            else
            {
                Session.SetError("Failed to apply theme.");
            }
        }
    }

    public string CurrentPassword { get => _currentPassword; set { if (SetProperty(ref _currentPassword, value)) Requery(); } }
    public string NewPassword { get => _newPassword; set { if (SetProperty(ref _newPassword, value)) Requery(); } }
    public string ConfirmPassword { get => _confirmPassword; set { if (SetProperty(ref _confirmPassword, value)) Requery(); } }

    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }

    public string Email => IsGuestMode ? "(Guest)" : TokenStore.Email ?? "Unknown";

    public RelayCommand ChangePasswordCommand { get; }
    public RelayCommand LogoutCommand { get; }

    private static void Requery() => System.Windows.Input.CommandManager.InvalidateRequerySuggested();

    private async Task ChangePasswordAsync()
    {
        Session.SetStatus("Changing password…");

        var result = await _api.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewPassword
        });

        if (!result.Success)
        {
            Session.SetError(result.Error ?? "Change failed.");
            return;
        }

        Session.SetStatus("Password changed.");
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    public (bool Success, string? Error) ExportLibrary(string destinationPath)
    {
        var store = new LocalLibraryStore();
        return store.ExportTo(destinationPath)
            ? (true, null)
            : (false, "Failed to export the library.");
    }

    public (bool Success, string? Error, int PlaylistsImported) ImportLibrary(string sourcePath)
    {
        var store = new LocalLibraryStore();
        return store.ImportFrom(sourcePath);
    }

    public Task<(bool Success, string? Error, string? BackupPath)> PrepareUpgradeAsync()
        => _nav.PrepareUpgradeAsync();

    public void BeginUpgrade(string backupPath) => _nav.BeginUpgrade(backupPath);

    private async Task LogoutAsync()
    {
        await _api.LogoutAsync();
        Session.SetStatus("Logging out...");
        _nav.Logout();
    }
}