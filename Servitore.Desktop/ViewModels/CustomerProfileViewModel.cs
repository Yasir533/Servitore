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
            int maxRetries = 15;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Profile = await _apiService.GetAsync<CustomerProfileDto>($"api/customers/{_customerId}/profile");
                    return; // Success!
                }
                catch (Exception ex)
                {
                    ClientLogger.Log($"Attempt {i + 1} to load customer profile data failed", ex);
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
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
    private void ViewProductDetails(CustomerProductDto? product)
    {
        if (product is null) return;
        NavigationHelper.NavigateTo(new Views.ProductProfileView(product.ProductId));
    }
}
