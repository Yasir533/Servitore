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
        _viewModel.IsBusy = true;
        ErrorText.Text = "Connecting to server...";
        ErrorText.Foreground = System.Windows.Media.Brushes.Gray;
        ErrorText.Visibility = Visibility.Visible;

        bool isOnline = false;
        int maxRetries = 10;
        for (int i = 0; i < maxRetries; i++)
        {
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
            await System.Threading.Tasks.Task.Delay(1000);
        }

        _viewModel.IsBusy = false;
        if (isOnline)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Foreground = (System.Windows.Media.Brush)FindResource("MaterialDesignValidationErrorBrush");
        }
        else
        {
            _viewModel.ErrorMessage = "Unable to connect to the server. Please ensure the server is running.";
            ErrorText.Text = _viewModel.ErrorMessage;
            ErrorText.Foreground = (System.Windows.Media.Brush)FindResource("MaterialDesignValidationErrorBrush");
            ErrorText.Visibility = Visibility.Visible;
        }
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

    private void OnLoginSucceeded()
    {
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
