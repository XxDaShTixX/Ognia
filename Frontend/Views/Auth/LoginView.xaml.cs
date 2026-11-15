using System.Windows.Controls;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Services;
using Ognia.Frontend.ViewModels;

namespace Ognia.Frontend.Views.Auth;

public partial class LoginView : UserControl
{
    private readonly LoginViewModel _vm;

    public LoginView(ApiClientService api, IAppNavigator nav)
    {
        InitializeComponent();
        _vm = new LoginViewModel(api, nav);
        DataContext = _vm;
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        => _vm.Password = ((PasswordBox)sender).Password;

    private void RegPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        => _vm.RegPassword = ((PasswordBox)sender).Password;
}