using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow()
    {
        InitializeComponent();

        _viewModel = new LoginViewModel(App.AuthenticationService);
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        DataContext = _viewModel;

        Loaded += LoginWindow_Loaded;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UsernameBox.Focus();
        _ = RunConnectionCheckInBackgroundAsync();
    }

    private bool _isServerOnline = false;

    private async System.Threading.Tasks.Task RunConnectionCheckInBackgroundAsync()
    {
        UpdateStatusLight(false, "Connecting to server...");
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] Starting background connection check. API base URL: {App.ApiService.BaseUrl}");

        int maxRetries = 60; // 30 seconds
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var pingResult = await App.ApiService.GetAsync<PingResponse>("api/auth/ping");
                if (pingResult is { Status: "Healthy" })
                {
                    _isServerOnline = true;
                    UpdateStatusLight(true, "Server Online");
                    Helpers.ClientLogger.Log("[DIAGNOSTIC] Background connection check: Server is ONLINE.");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log($"[DIAGNOSTIC] Startup connection check attempt {i + 1} failed. Endpoint: {App.ApiService.BaseUrl}api/auth/ping", ex);
            }
            
            await System.Threading.Tasks.Task.Delay(1000);
        }

        UpdateStatusLight(false, "Server Offline. Check API connection.", isFailed: true);
        Helpers.ClientLogger.Log("[DIAGNOSTIC] Background connection check: Server is OFFLINE after 60 retries.");
    }

    private void UpdateStatusLight(bool isOnline, string text, bool isFailed = false)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = text;
            if (isOnline)
            {
                StatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
            }
            else if (isFailed)
            {
                StatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
            }
            else
            {
                StatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 179, 0)); // Amber
            }
        });
    }

    private class PingResponse
    {
        public string Status { get; set; } = string.Empty;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isServerOnline)
        {
            try
            {
                Helpers.ClientLogger.Log("[DIAGNOSTIC] LoginButton clicked, server not online yet. Pinging server...");
                var pingResult = await App.ApiService.GetAsync<PingResponse>("api/auth/ping");
                if (pingResult is { Status: "Healthy" })
                {
                    _isServerOnline = true;
                    UpdateStatusLight(true, "Server Online");
                    Helpers.ClientLogger.Log("[DIAGNOSTIC] Ping during LoginButton click succeeded: Server is ONLINE.");
                }
            }
            catch (Exception ex)
            {
                Helpers.ClientLogger.Log("[DIAGNOSTIC] Ping during LoginButton click failed.", ex);
            }
        }

        if (!_isServerOnline)
        {
            ErrorText.Text = "Cannot log in because the server is offline. Please check your connection.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        // PasswordBox cannot be data-bound; pass value manually before executing the command.
        _viewModel.Username = UsernameBox.Text.Trim();
        _viewModel.Password = PasswordBox.Password;

        ErrorText.Visibility = Visibility.Collapsed;

        await _viewModel.LoginCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
        {
            ErrorText.Text = _viewModel.ErrorMessage;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            LoginButton_Click(sender, new RoutedEventArgs());
    }

    private void OnLoginSucceeded()
    {
        var token = App.AuthenticationService.CurrentToken;
        if (!string.IsNullOrEmpty(token))
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await App.SignalRService.ConnectAsync(App.ApiService.BaseUrl, token);
                }
                catch (System.Exception ex)
                {
                    Helpers.ClientLogger.Log("Failed to start SignalR connection on login", ex);
                }
            });
        }

        var dashboard = new DashboardView();
        var username = App.AuthenticationService.CurrentUser?.Username ?? "admin";
        var mainWindow = new Window
        {
            Title = $"Servitore - Sai services - {username}",
            Content = dashboard,
            WindowState = WindowState.Maximized,
            Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/Icons/logo.ico", UriKind.Absolute))
        };
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();

        Close();
    }
}
