using System;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _viewModel;
    private System.Windows.Threading.DispatcherTimer? _idleTimer;
    private bool _isAway = false;
    private string _currentTag = "Dashboard";

    public DashboardView()
    {
        InitializeComponent();
        NavigationHelper.Initialize(ContentHost);

        _viewModel = new DashboardViewModel(App.ApiService);
        DataContext = _viewModel;

        // Register Toast helper
        ToastHelper.MessageQueue = MainSnackbar.MessageQueue;

        // Enforce role-based menu hiding
        var role = App.AuthenticationService.CurrentUser?.Role;
        if (role == Servitore.Shared.Enums.UserRole.Engineer || role == Servitore.Shared.Enums.UserRole.Operator)
        {
            UsersBtn.Visibility = Visibility.Collapsed;
            SettingsBtn.Visibility = Visibility.Collapsed;
        }

        if (role != Servitore.Shared.Enums.UserRole.Admin)
        {
            ActivityLogsBtn.Visibility = Visibility.Collapsed;
        }

        Loaded += DashboardView_Loaded;
        Unloaded += DashboardView_Unloaded;

        App.SignalRService.LockTakenOver += OnLockTakenOver;
        App.SignalRService.DataChanged += OnDataChanged;
        App.SignalRService.ActivityLogged += OnActivityLogged;
        App.SignalRService.ForceLogoutReceived += OnForceLogout;

        ShowSummary();
    }

    private bool _updatingStatusFromCode = false;
    private string _manualStatus = "Online";

    private void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewMouseMove += Window_Activity;
            window.PreviewKeyDown += Window_Activity;
        }

        _idleTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(App.ApiService.IdleTimeoutMinutes)
        };
        _idleTimer.Tick += IdleTimer_Tick;
        _idleTimer.Start();

        _updatingStatusFromCode = true;
        StatusSelectorCombo.SelectedIndex = 0; // Default: Online
        _updatingStatusFromCode = false;

        _ = App.SignalRService.UpdatePresenceAsync(_currentTag, "Online");
    }

    private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewMouseMove -= Window_Activity;
            window.PreviewKeyDown -= Window_Activity;
        }

        if (_idleTimer != null)
        {
            _idleTimer.Stop();
            _idleTimer = null;
        }

        App.SignalRService.LockTakenOver -= OnLockTakenOver;
        App.SignalRService.DataChanged -= OnDataChanged;
        App.SignalRService.ActivityLogged -= OnActivityLogged;
        App.SignalRService.ForceLogoutReceived -= OnForceLogout;
    }

    private void Window_Activity(object sender, System.Windows.Input.InputEventArgs e)
    {
        _idleTimer?.Stop();
        _idleTimer?.Start();

        if (_isAway)
        {
            _isAway = false;
            if (_manualStatus == "Online")
            {
                _updatingStatusFromCode = true;
                StatusSelectorCombo.SelectedIndex = 0; // Online
                _updatingStatusFromCode = false;
                _ = App.SignalRService.UpdatePresenceAsync(_currentTag, "Online");
            }
        }
    }

    private void IdleTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isAway && _manualStatus == "Online")
        {
            _isAway = true;
            _updatingStatusFromCode = true;
            StatusSelectorCombo.SelectedIndex = 1; // Away
            _updatingStatusFromCode = false;
            _ = App.SignalRService.UpdatePresenceAsync(_currentTag, "Away");
        }
    }

    private void StatusSelectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingStatusFromCode) return;
        if (StatusSelectorCombo.SelectedItem is ComboBoxItem item && item.Tag is string status)
        {
            _manualStatus = status;
            _isAway = (status == "Away");
            _ = App.SignalRService.UpdatePresenceAsync(_currentTag, status);
        }
    }

    private void OnLockTakenOver(string recordKey, string newOwner)
    {
        Dispatcher.Invoke(() =>
        {
            ToastHelper.ShowToast($"Lock on {recordKey} was taken over by {newOwner}.");
        });
    }

    private void OnDataChanged(Servitore.Shared.Models.DataEventModel dataEvent)
    {
        Dispatcher.Invoke(() =>
        {
            ToastHelper.ShowToast($"{dataEvent.EntityType} '{dataEvent.DisplayName}' was {dataEvent.Action.ToLower()} by {dataEvent.Username}.");
        });
    }

    private void OnActivityLogged(Servitore.Shared.Models.ActivityLogDto activity)
    {
        Dispatcher.Invoke(() =>
        {
            if (_viewModel.Summary?.RecentActivities != null)
            {
                _viewModel.Summary.RecentActivities.Insert(0, activity);
                if (_viewModel.Summary.RecentActivities.Count > 10)
                {
                    _viewModel.Summary.RecentActivities.RemoveAt(10);
                }
                _viewModel.NotifyActivityAdded();
            }
        });
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag }) return;

        _currentTag = tag;
        _ = App.SignalRService.UpdatePresenceAsync(tag, _isAway ? "Away" : "Online");

        if (tag == "Dashboard")
        {
            ShowSummary();
            return;
        }

        UserControl view = tag switch
        {
            "Customers"      => new CustomerView(),
            "Assets"         => new AssetView(),
            "ServiceTickets" => new ServiceTicketView(),
            "Warranty"       => new WarrantyView(),
            "AMC"            => new AMCView(),
            "Reports"        => new ReportsView(),
            "Users"          => new UserManagementView(),
            "ActivityLogs"   => new ActivityLogView(),
            "Settings"       => new SettingsView(),
            "LiveUsers"      => new LiveUsersView(),
            _                => new CustomerView()
        };

        NavigationHelper.NavigateTo(ContentHost, view);
    }

    private async void ShowSummary()
    {
        try
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (System.Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to load dashboard summary", ex);
        }
        var homeView = new DashboardHomeView { DataContext = _viewModel };
        ContentHost.Content = homeView;
    }

    private void NotificationBtn_Click(object sender, RoutedEventArgs e)
    {
        NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
    }

    private void GlobalSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            var text = GlobalSearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Navigate to Service Tickets view and pre-fill search text
            var ticketView = new ServiceTicketView();
            if (ticketView.DataContext is ServiceTicketViewModel ticketVm)
            {
                ticketVm.SearchText = text;
            }
            NavigationHelper.NavigateTo(ticketView);
        }
    }

    private void OnForceLogout()
    {
        Dispatcher.Invoke(async () =>
        {
            MessageBox.Show("An Administrator has forced your session to log out.", "Forced Logout", MessageBoxButton.OK, MessageBoxImage.Warning);
            App.AuthenticationService.Logout();
            await App.SignalRService.DisconnectAsync();

            var loginWin = new LoginWindow();
            loginWin.Show();

            var currentWin = Window.GetWindow(this);
            currentWin?.Close();
        });
    }
}
