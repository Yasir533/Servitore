using System;
using System.Threading.Tasks;
using System.Windows;
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

    // Category Breakdown Percentages & Counts
    [ObservableProperty]
    private int _warrantyCount;
    [ObservableProperty]
    private int _oowCount;
    [ObservableProperty]
    private int _amcCount;

    [ObservableProperty]
    private double _warrantyPercentage;
    [ObservableProperty]
    private double _oowPercentage;
    [ObservableProperty]
    private double _amcPercentage;

    [ObservableProperty]
    private GridLength _warrantyColumnWidth = new GridLength(1, GridUnitType.Star);
    [ObservableProperty]
    private GridLength _oowColumnWidth = new GridLength(1, GridUnitType.Star);
    [ObservableProperty]
    private GridLength _amcColumnWidth = new GridLength(1, GridUnitType.Star);

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
            Summary = await _apiService.GetAsync<DashboardSummary>("api/dashboard/summary");
            ComputeChartMetrics();
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to load dashboard summary", ex);
            Helpers.ToastHelper.ShowToast("Failed to refresh dashboard summary.");
            Summary = new DashboardSummary();
            ComputeChartMetrics();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeChartMetrics()
    {
        if (Summary == null) return;

        // 1. Scale Monthly Resolve Rates
        int maxCount = 0;
        foreach (var item in Summary.MonthlyResolveRates)
        {
            if (item.Count > maxCount) maxCount = item.Count;
        }
        foreach (var item in Summary.MonthlyResolveRates)
        {
            item.BarHeight = maxCount > 0 ? ((double)item.Count / maxCount) * 120.0 : 0.0;
            if (item.Count > 0 && item.BarHeight < 6.0) item.BarHeight = 6.0;
        }

        // 2. Compute category counts & percentages
        Summary.CategoryCounts.TryGetValue("Warranty", out var warranty);
        Summary.CategoryCounts.TryGetValue("OOW", out var oow);
        Summary.CategoryCounts.TryGetValue("AMC", out var amc);

        WarrantyCount = warranty;
        OowCount = oow;
        AmcCount = amc;

        int total = warranty + oow + amc;
        if (total > 0)
        {
            WarrantyPercentage = Math.Round(((double)warranty / total) * 100, 1);
            OowPercentage = Math.Round(((double)oow / total) * 100, 1);
            AmcPercentage = Math.Round(((double)amc / total) * 100, 1);

            WarrantyColumnWidth = new GridLength(warranty > 0 ? warranty : 0.0001, GridUnitType.Star);
            OowColumnWidth = new GridLength(oow > 0 ? oow : 0.0001, GridUnitType.Star);
            AmcColumnWidth = new GridLength(amc > 0 ? amc : 0.0001, GridUnitType.Star);
        }
        else
        {
            WarrantyPercentage = 0;
            OowPercentage = 0;
            AmcPercentage = 0;

            WarrantyColumnWidth = new GridLength(1, GridUnitType.Star);
            OowColumnWidth = new GridLength(1, GridUnitType.Star);
            AmcColumnWidth = new GridLength(1, GridUnitType.Star);
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
