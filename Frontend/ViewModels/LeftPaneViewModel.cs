using Ognia.Frontend.Enums;
using Ognia.Frontend.Helpers;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Session;

namespace Ognia.Frontend.ViewModels;

public sealed class LeftPaneViewModel : ViewModelBase
{
    private readonly IShellNavigator _nav;

    public LeftPaneViewModel(AppSession session, IShellNavigator nav)
    {
        _nav = nav;
        Session = session;

        LibraryCommand = new RelayCommand(_ => _nav.ShowLibrary());
        SearchCommand = new RelayCommand(_ => _nav.ShowSearch());
        SettingsCommand = new RelayCommand(_ => _nav.ShowSettings());
    }

    public AppSession Session { get; }

    public RelayCommand LibraryCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand SettingsCommand { get; }

    public string WelcomeText
    {
        get
        {
            var who = AppSettings.Instance.Mode == AppMode.Guest
                ? "Guest"
                : TokenStore.Email ?? "Guest";
            return $"Welcome, {who}";
        }
    }
}