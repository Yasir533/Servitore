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
        await CheckConnectionAndSetupUiAsync();
    }

    private async System.Threading.Tasks.Task CheckConnectionAndSetupUiAsync()
    {
        // 1. Optimize for instant load: Try to ping immediately first
        try
        {
            var pingResult = await App.ApiService.GetAsync<PingResponse>("api/auth/ping");
            if (pingResult is { Status: "Healthy" })
            {
                ConnectionCheckPanel.Visibility = Visibility.Collapsed;
                LoginFormPanel.Visibility = Visibility.Visible;
                ConnectionFailedPanel.Visibility = Visibility.Collapsed;
                UsernameBox.Focus();
                return;
            }
        }
        catch (System.Exception ex)
        {
            Helpers.ClientLogger.Log("Instant connection check failed, initiating retry sequence.", ex);
        }

        // 2. Slow path: Show Loading UI & attempt retries
        ConnectionCheckPanel.Visibility = Visibility.Visible;
        LoginFormPanel.Visibility = Visibility.Collapsed;
        ConnectionFailedPanel.Visibility = Visibility.Collapsed;

        bool isOnline = false;
        int maxRetries = 60; // 60 attempts * 500ms = 30 seconds timeout
        for (int i = 0; i < maxRetries; i++)
        {
            // Progressive status text updates based on elapsed time (5s / 15s boundaries)
            if (i < 10)
            {
                ConnectionStatusText.Text = "Starting Servitore...";
            }
            else if (i < 30)
            {
                ConnectionStatusText.Text = "Connecting to server...";
            }
            else
            {
                ConnectionStatusText.Text = "Loading data...";
            }

            try
            {
                var pingResult = await App.ApiService.GetAsync<PingResponse>("api/auth/ping");
                if (pingResult is { Status: "Healthy" })
                {
                    isOnline = true;
                    break;
                }
            }
            catch (System.Exception ex)
            {
                Helpers.ClientLogger.Log($"Startup connection attempt {i + 1} failed.", ex);
            }
            await System.Threading.Tasks.Task.Delay(500);
        }

        if (isOnline)
        {
            ConnectionCheckPanel.Visibility = Visibility.Collapsed;
            LoginFormPanel.Visibility = Visibility.Visible;
            ConnectionFailedPanel.Visibility = Visibility.Collapsed;
            UsernameBox.Focus();
        }
        else
        {
            var result = MessageBox.Show(
                this,
                "Unable to connect to the server. Please ensure the Servitore API is running.",
                "Connection Failed",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                await CheckConnectionAndSetupUiAsync();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckConnectionAndSetupUiAsync();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private class PingResponse
    {
        public string Status { get; set; } = string.Empty;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
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

    private async void OnLoginSucceeded()
    {
        try
        {
            var token = App.AuthenticationService.CurrentToken;
            if (!string.IsNullOrEmpty(token))
            {
                await App.SignalRService.ConnectAsync(App.ApiService.BaseUrl, token);
            }
        }
        catch (System.Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to start SignalR connection on login", ex);
        }

        var dashboard = new DashboardView();
        var mainWindow = new Window
        {
            Title = "Servitore - Dashboard",
            Content = dashboard,
            WindowState = WindowState.Maximized
        };
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();

        Close();
    }
}
