using System;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _viewModel;
    private System.Windows.Threading.DispatcherTimer? _searchDebounceTimer;
    private string _currentTag = "Dashboard";

    public DashboardView()
    {
        InitializeComponent();
        NavigationHelper.Initialize(ContentHost);

        _viewModel = new DashboardViewModel(App.ApiService);
        DataContext = _viewModel;

        _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

        // Register Toast helper
        ToastHelper.MessageQueue = MainSnackbar.MessageQueue;

        // Enforce role-based menu hiding
        var role = App.AuthenticationService.CurrentUser?.Role;
        if (role == Servitore.Shared.Enums.UserRole.Engineer || role == Servitore.Shared.Enums.UserRole.Operator)
        {
            UsersBtn.Visibility = Visibility.Collapsed;
            SettingsBtn.Visibility = Visibility.Collapsed;
            RecentlyDeletedBtn.Visibility = Visibility.Collapsed;
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
        App.SignalRService.Reconnecting += OnSignalRReconnecting;
        App.SignalRService.Reconnected += OnSignalRReconnected;
        App.SignalRService.Closed += OnSignalRClosed;

        ShowSummary();
    }

    private void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        App.SignalRService.CurrentStatusChanged += OnSignalRStatusChanged;
        OnSignalRStatusChanged(App.SignalRService.CurrentStatus);

        App.SignalRService.UpdateCurrentModule("Dashboard");
    }

    private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
    {
        App.SignalRService.LockTakenOver -= OnLockTakenOver;
        App.SignalRService.DataChanged -= OnDataChanged;
        App.SignalRService.ActivityLogged -= OnActivityLogged;
        App.SignalRService.ForceLogoutReceived -= OnForceLogout;
        App.SignalRService.Reconnecting -= OnSignalRReconnecting;
        App.SignalRService.Reconnected -= OnSignalRReconnected;
        App.SignalRService.Closed -= OnSignalRClosed;

        App.SignalRService.CurrentStatusChanged -= OnSignalRStatusChanged;
    }

    private void OnSignalRStatusChanged(string status)
    {
        Dispatcher.Invoke(() =>
        {
            CurrentUserStatusText.Text = status;
            switch (status)
            {
                case "Online":
                    CurrentUserStatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                    break;
                case "Busy":
                    CurrentUserStatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
                    break;
                default:
                    CurrentUserStatusLight.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158));
                    break;
            }
        });
    }

    private void OnSignalRReconnecting(string? message)
    {
        Dispatcher.Invoke(() =>
        {
            ToastHelper.ShowToast("Connection lost. Reconnecting to server...");
        });
    }

    private void OnSignalRReconnected(string? connectionId)
    {
        Dispatcher.Invoke(() =>
        {
            ToastHelper.ShowToast("Connected to server.");
            ShowSummary();
        });
    }

    private void OnSignalRClosed(Exception? exception)
    {
        Dispatcher.Invoke(() =>
        {
            ToastHelper.ShowToast("Offline. Connection to server closed.");
        });
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
        App.SignalRService.UpdateCurrentModule(tag);

        if (tag == "Dashboard")
        {
            ShowSummary();
            return;
        }

        try
        {
            UserControl view = tag switch
            {
                "Customers"      => new CustomerView(),
                "Products"       => new ProductView(),
                "ServiceEntries" => new ServiceEntryView(),
                "Reports"        => new ReportsView(),
                "Users"          => new UserManagementView(),
                "ActivityLogs"   => new ActivityLogView(),
                "Settings"       => new SettingsView(),
                "LiveUsers"      => new LiveUsersView(),
                "RecentlyDeleted"=> new RecentlyDeletedView(),
                _                => new CustomerView()
            };

            NavigationHelper.NavigateTo(ContentHost, view);
        }
        catch (Exception ex)
        {
            ClientLogger.Log($"Navigation to {tag} failed", ex);
            DialogHelper.ShowError($"Failed to load page '{tag}': {ex.Message}", "Navigation Error");
        }
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

            SearchAutocompletePopup.IsOpen = false;
            var view = new ServiceEntryView();
            if (view.DataContext is ServiceEntryViewModel vm)
            {
                vm.SearchText = text;
            }
            NavigationHelper.NavigateTo(ContentHost, view);
        }
    }

    private void GlobalSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer?.Start();
    }

    private async void SearchDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _searchDebounceTimer?.Stop();
        var query = GlobalSearchBox.Text.Trim();
        if (query.Length < 2)
        {
            SearchAutocompletePopup.IsOpen = false;
            return;
        }

        try
        {
            var results = await App.ApiService.GetAsync<Servitore.Shared.Models.SearchResultDto>($"api/search?q={Uri.EscapeDataString(query)}");
            if (results != null)
            {
                var flatList = new System.Collections.Generic.List<SearchDropdownItem>();
                foreach (var item in results.Customers)
                {
                    flatList.Add(new SearchDropdownItem { Id = item.Id, Title = item.Title, Subtitle = item.Subtitle, Type = "Customer" });
                }
                foreach (var item in results.Products)
                {
                    flatList.Add(new SearchDropdownItem { Id = item.Id, Title = item.Title, Subtitle = item.Subtitle, Type = "Product" });
                }
                foreach (var item in results.ServiceEntries)
                {
                    flatList.Add(new SearchDropdownItem { Id = item.Id, Title = item.Title, Subtitle = item.Subtitle, Type = "Service Entry" });
                }
                foreach (var item in results.Employees)
                {
                    flatList.Add(new SearchDropdownItem { Id = item.Id, Title = item.Title, Subtitle = item.Subtitle, Type = "Employee" });
                }

                if (flatList.Count > 0)
                {
                    SearchResultsList.ItemsSource = flatList;
                    SearchAutocompletePopup.IsOpen = true;
                }
                else
                {
                    SearchAutocompletePopup.IsOpen = false;
                }
            }
            else
            {
                SearchAutocompletePopup.IsOpen = false;
            }
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to perform global search", ex);
            SearchAutocompletePopup.IsOpen = false;
        }
    }

    private async void SearchResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SearchDropdownItem item)
        {
            SearchAutocompletePopup.IsOpen = false;
            GlobalSearchBox.Text = "";

            try
            {
                if (item.Type == "Customer")
                {
                    NavigationHelper.NavigateTo(ContentHost, new CustomerProfileView(int.Parse(item.Id)));
                }
                else if (item.Type == "Product")
                {
                    NavigationHelper.NavigateTo(ContentHost, new ProductProfileView(int.Parse(item.Id)));
                }
                else if (item.Type == "Service Entry")
                {
                    var entryId = int.Parse(item.Id);
                    var entryDetails = await App.ApiService.GetAsync<Servitore.Shared.Models.ServiceEntryDetailsDto>($"api/serviceentries/{entryId}");
                    if (entryDetails != null)
                    {
                        var dialog = new Views.Dialogs.ServiceEntryEditDialog(App.ApiService, entryDetails)
                        {
                            Owner = Window.GetWindow(this)
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            if (DataContext is ViewModels.DashboardViewModel vm)
                            {
                                vm.LoadCommand.Execute(null);
                            }
                        }
                    }
                }
                else if (item.Type == "Employee")
                {
                    NavigationHelper.NavigateTo(ContentHost, new UserManagementView());
                }
            }
            catch (Exception ex)
            {
                Helpers.ClientLogger.Log("Failed to navigate from search result", ex);
                DialogHelper.ShowError("Could not load details for the selected item.");
            }
        }
    }

    public class SearchDropdownItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    private void OnForceLogout()
    {
        Dispatcher.Invoke(async () =>
        {
            DialogHelper.ShowError("An Administrator has forced your session to log out.", "Forced Logout");
            App.AuthenticationService.Logout();
            await App.SignalRService.DisconnectAsync();

            var loginWin = new LoginWindow();
            loginWin.Show();

            var currentWin = Window.GetWindow(this);
            currentWin?.Close();
        });
    }
}
