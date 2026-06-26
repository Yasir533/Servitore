using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;
using Servitore.Desktop.Helpers;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoNotifications))]
    [NotifyPropertyChangedFor(nameof(HasNoActivities))]
    private DashboardSummary? summary;

    [ObservableProperty]
    private bool isLoading;

    public bool HasNoNotifications =>
        Summary == null || Summary.RecentNotifications.Count == 0;

    public bool HasNoActivities =>
        Summary == null || Summary.RecentActivities.Count == 0;

    public void NotifyActivityAdded()
    {
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(HasNoActivities));
    }

    public DashboardViewModel(ApiService apiService) => _apiService = apiService;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            int maxRetries = 15;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Summary = await _apiService.GetAsync<DashboardSummary>("api/dashboard/summary");
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load dashboard summary failed", ex);
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
            Summary = new DashboardSummary();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCustomerAsync()
    {
        var dialog = new Views.Dialogs.CustomerEditDialog(null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        dialog.ShowDialog();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        var dialog = new Views.Dialogs.ProductEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        dialog.ShowDialog();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CreateServiceEntryAsync()
    {
        var dialog = new Views.Dialogs.ServiceEntryEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                await LoadAsync();
            }
            catch (Exception)
            {
                Helpers.DialogHelper.ShowError("Unable to refresh dashboard data.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private void ViewActivityLogs()
    {
        NavigationHelper.NavigateTo(new Views.ActivityLogView());
    }
}
