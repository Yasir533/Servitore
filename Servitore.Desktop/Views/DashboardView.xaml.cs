using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _viewModel;

    public DashboardView()
    {
        InitializeComponent();
        NavigationHelper.Initialize(ContentHost);

        _viewModel = new DashboardViewModel(App.ApiService);
        DataContext = _viewModel;

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

        ShowSummary();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag }) return;

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
            _                => new CustomerView()
        };

        NavigationHelper.NavigateTo(ContentHost, view);
    }

    private async void ShowSummary()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
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
}
