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
    private bool _isDatabaseOnline = false;

    private enum StatusState
    {
        Connecting,
        Connected,
        Disconnected,
        ServerOffline,
        DatabaseOffline
    }

    private async System.Threading.Tasks.Task RunConnectionCheckInBackgroundAsync()
    {
        UpdateStatusLight(StatusState.Connecting, "Connecting to server...");
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] Starting background connection check. API base URL: {App.ApiService.BaseUrl}");

        int maxRetries = 60; // 60 seconds
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var pingResult = await App.ApiService.GetAsync<PingResponse>("api/auth/ping");
                if (pingResult is not null)
                {
                    if (pingResult.Server == "Online" && pingResult.Database == "Online")
                    {
                        _isServerOnline = true;
                        _isDatabaseOnline = true;
                        UpdateStatusLight(StatusState.Connected, "Connected");
                        Helpers.ClientLogger.Log("[DIAGNOSTIC] Background connection check: Connected.");
                        return;
                    }
                    else if (pingResult.Server == "Online")
                    {
                        _isServerOnline = true;
                        _isDatabaseOnline = false;
                        UpdateStatusLight(StatusState.DatabaseOffline, "Database Offline");
                        Helpers.ClientLogger.Log("[DIAGNOSTIC] Background connection check: Database is offline.");
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _isServerOnline = false;
                _isDatabaseOnline = false;
                UpdateStatusLight(StatusState.ServerOffline, "Server Offline");
                Helpers.ClientLogger.Log($"[DIAGNOSTIC] Startup connection check attempt {i + 1} failed. Server offline. {ex.Message}");
            }
            catch (System.Exception ex)
            {
                _isServerOnline = false;
                _isDatabaseOnline = false;
                UpdateStatusLight(StatusState.Disconnected, "Disconnected");
                Helpers.ClientLogger.Log($"[DIAGNOSTIC] Startup connection check attempt {i + 1} failed. {ex.Message}");
            }
            
            await System.Threading.Tasks.Task.Delay(1000);
        }

        if (!_isServerOnline)
        {
            UpdateStatusLight(StatusState.ServerOffline, "Server Offline. Check API connection.");
        }
        else if (!_isDatabaseOnline)
        {
            UpdateStatusLight(StatusState.DatabaseOffline, "Database Offline");
        }
        else
        {
            UpdateStatusLight(StatusState.Disconnected, "Disconnected");
        }
    }

    private void UpdateStatusLight(StatusState state, string text)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = text;
            switch (state)
            {
                case StatusState.Connected:
                    StatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                    break;
                case StatusState.Connecting:
                case StatusState.Disconnected:
                    StatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 179, 0)); // Amber
                    break;
                case StatusState.ServerOffline:
                case StatusState.DatabaseOffline:
                    StatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                    break;
            }
        });
    }

    private class PingResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isServerOnline || !_isDatabaseOnline)
        {
            try
            {
                Helpers.ClientLogger.Log("[DIAGNOSTIC] LoginButton clicked, server or database not online yet. Pinging server...");
                var pingResult = await App.ApiService.GetAsync<PingResponse>("api/auth/ping");
                if (pingResult is not null)
                {
                    _isServerOnline = pingResult.Server == "Online";
                    _isDatabaseOnline = pingResult.Database == "Online";
                    if (_isServerOnline && _isDatabaseOnline)
                    {
                        UpdateStatusLight(StatusState.Connected, "Connected");
                    }
                    else if (_isServerOnline)
                    {
                        UpdateStatusLight(StatusState.DatabaseOffline, "Database Offline");
                    }
                }
            }
            catch (Exception ex)
            {
                _isServerOnline = false;
                _isDatabaseOnline = false;
                Helpers.ClientLogger.Log("[DIAGNOSTIC] Ping during LoginButton click failed.", ex);
            }
        }

        if (!_isServerOnline)
        {
            ErrorText.Text = "Cannot log in because the server is offline. Please check your connection.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (!_isDatabaseOnline)
        {
            ErrorText.Text = "Cannot log in because the database is unavailable. Please contact the administrator.";
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
