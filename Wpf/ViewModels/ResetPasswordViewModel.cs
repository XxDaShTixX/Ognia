using Ognia.Wpf.Models.Auth;
using Ognia.Wpf.Navigation;
using Ognia.Wpf.Services;

namespace Ognia.Wpf.ViewModels;

public class ResetPasswordViewModel : ViewModelBase
{
    private readonly ApiClientService _api;

    private string _email = string.Empty;
    private string _token = string.Empty;
    private string _newPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isError;

    public ResetPasswordViewModel(ApiClientService api, IAppNavigator nav)
    {
        _api = api;

        RequestTokenCommand = new RelayCommand(
            async _ => await RequestTokenAsync(),
            _ => !string.IsNullOrWhiteSpace(Email));

        ResetPasswordCommand = new RelayCommand(
            async _ => await ResetPasswordAsync(),
            _ => !string.IsNullOrWhiteSpace(Email)
                 && !string.IsNullOrWhiteSpace(Token)
                 && !string.IsNullOrWhiteSpace(NewPassword));

        BackCommand = new RelayCommand(_ => nav.ShowLogin());
    }

    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Token { get => _token; set => SetProperty(ref _token, value); }
    public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }

    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }

    public RelayCommand RequestTokenCommand { get; }
    public RelayCommand ResetPasswordCommand { get; }
    public RelayCommand BackCommand { get; }

    private async Task RequestTokenAsync()
    {
        IsError = false;
        StatusMessage = "Requesting…";

        var result = await _api.ForgotPasswordAsync(new ForgotPasswordRequest { Email = Email.Trim() });
        if (!result.Success)
        {
            IsError = true;
            StatusMessage = result.Error ?? "Request failed.";
            return;
        }

        StatusMessage = "If that email exists, a reset token was printed to the server console. Paste it below.";
    }

    private async Task ResetPasswordAsync()
    {
        IsError = false;
        StatusMessage = "Resetting…";

        var result = await _api.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = Email.Trim(),
            Token = Token.Trim(),
            NewPassword = NewPassword
        });

        if (!result.Success)
        {
            IsError = true;
            StatusMessage = result.Error ?? "Reset failed.";
            return;
        }

        StatusMessage = "Password reset. You can log in now.";
        Token = string.Empty;
        NewPassword = string.Empty;
    }
}