using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthenticationService _authService;

    [ObservableProperty]
    private string username = string.Empty;

    // Password is NOT data-bound directly (PasswordBox doesn't support binding for security).
    // The code-behind sets this property before executing LoginCommand.
    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Fired on the UI thread when login succeeds.</summary>
    public event Action? LoginSucceeded;

    public LoginViewModel(AuthenticationService authService) => _authService = authService;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (result is { Success: true })
            {
                LoginSucceeded?.Invoke();
            }
            else
            {
                ErrorMessage = result?.Message ?? "Login failed. Please try again.";
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to reach server. Please try again later.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();
}
