using Ognia.Frontend.Helpers;
using Ognia.Frontend.Models.Auth;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Repos;
using Ognia.Frontend.Services;

namespace Ognia.Frontend.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly ApiClientService _api;
    private readonly IAppNavigator _nav;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _regEmail = string.Empty;
    private string _regPassword = string.Empty;
    private string _loginStatus = string.Empty;
    private string _registerStatus = string.Empty;
    private bool _isLoginError;
    private bool _isRegisterError;
    private bool _showLoginForm;

    public LoginViewModel(ApiClientService api, IAppNavigator nav)
    {
        _api = api;
        _nav = nav;

        LoginCommand = new RelayCommand(
            async _ => await LoginAsync(),
            _ => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password));

        SignUpCommand = new RelayCommand(
            async _ => await SignUpAsync(),
            _ => !string.IsNullOrWhiteSpace(RegEmail) && !string.IsNullOrWhiteSpace(RegPassword));

        GuestCommand = new RelayCommand(_ => _nav.EnterGuestMode());
        ForgotPasswordCommand = new RelayCommand(_ => _nav.ShowResetPassword());

        ShowLoginFormCommand = new RelayCommand(_ => ShowLoginForm = true);
        BackToLandingCommand = new RelayCommand(_ => ShowLoginForm = false);
    }

    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string RegEmail { get => _regEmail; set => SetProperty(ref _regEmail, value); }
    public string RegPassword { get => _regPassword; set => SetProperty(ref _regPassword, value); }

    public string LoginStatus { get => _loginStatus; set => SetProperty(ref _loginStatus, value); }
    public string RegisterStatus { get => _registerStatus; set => SetProperty(ref _registerStatus, value); }

    public bool IsLoginError { get => _isLoginError; set => SetProperty(ref _isLoginError, value); }
    public bool IsRegisterError { get => _isRegisterError; set => SetProperty(ref _isRegisterError, value); }

    public bool ShowLoginForm
    {
        get => _showLoginForm;
        set
        {
            if (!SetProperty(ref _showLoginForm, value)) return;
            OnPropertyChanged(nameof(ShowLanding));
        }
    }

    public bool ShowLanding => !_showLoginForm;

    public RelayCommand LoginCommand { get; }
    public RelayCommand SignUpCommand { get; }
    public RelayCommand GuestCommand { get; }
    public RelayCommand ForgotPasswordCommand { get; }
    public RelayCommand ShowLoginFormCommand { get; }
    public RelayCommand BackToLandingCommand { get; }

    private async Task LoginAsync()
    {
        IsLoginError = false;
        LoginStatus = "Connecting...";

        var result = await _api.LoginAsync(new LoginRequest
        {
            Email = Email.Trim(),
            Password = Password
        });

        if (!result.Success || result.Data == null)
        {
            IsLoginError = true;
            LoginStatus = result.Error ?? "Login failed.";
            return;
        }

        TokenStore.Set(result.Data.Token, result.Data.Email, result.Data.AccessTokenExpiresAt, result.Data.RefreshToken);

        if (_nav.HasPendingUpgrade)
            await _nav.CompleteUpgradeAsync();
        else
            _nav.ShowShell(new ApiMusicRepo(_api));
    }

    private async Task SignUpAsync()
    {
        IsRegisterError = false;
        RegisterStatus = "Connecting...";

        var result = await _api.RegisterAsync(new RegisterRequest
        {
            Email = RegEmail.Trim(),
            Password = RegPassword
        });

        if (!result.Success)
        {
            IsRegisterError = true;
            RegisterStatus = result.Error ?? "Registration failed.";
            return;
        }

        RegisterStatus = "Account created. You can log in now.";
    }
}