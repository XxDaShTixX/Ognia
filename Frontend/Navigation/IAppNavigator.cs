using Ognia.Frontend.Repos;

namespace Ognia.Frontend.Navigation;

public interface IAppNavigator : IShellNavigator
{
    void ShowShell(IMusicRepo repo);
    void DisposeShell();

    void EnterGuestMode();
    void ShowLogin();
    void ShowResetPassword();
    void Logout();

    bool HasPendingUpgrade { get; }
    Task CompleteUpgradeAsync();

    Task<(bool Success, string? Error, string? BackupPath)> PrepareUpgradeAsync();
    void BeginUpgrade(string backupPath);
}