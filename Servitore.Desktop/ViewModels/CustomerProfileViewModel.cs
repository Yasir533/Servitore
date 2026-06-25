using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class CustomerProfileViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly int _customerId;

    [ObservableProperty]
    private CustomerProfileDto? profile;

    [ObservableProperty]
    private bool isLoading;

    public CustomerProfileViewModel(ApiService apiService, int customerId)
    {
        _apiService = apiService;
        _customerId = customerId;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Profile = await _apiService.GetAsync<CustomerProfileDto>($"api/customers/{_customerId}/profile");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"Failed to load customer profile: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        NavigationHelper.NavigateTo(new Views.CustomerView());
    }

    [RelayCommand]
    private void ViewAssetDetails(CustomerAssetDto? asset)
    {
        if (asset is null) return;
        NavigationHelper.NavigateTo(new Views.AssetProfileView(asset.AssetId));
    }
}
