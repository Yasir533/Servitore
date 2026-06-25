using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Services;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class WarrantyEditDialog : Window
{
    private readonly ApiService _apiService;
    public WarrantyViewModel.WarrantyRow Warranty { get; }
    private bool _isLoaded = false;

    public WarrantyEditDialog(ApiService apiService, WarrantyViewModel.WarrantyRow? warranty = null)
    {
        InitializeComponent();
        _apiService = apiService;

        if (warranty != null)
        {
            Warranty = warranty;
            TitleText.Text = "Edit Warranty Details";
            StartDatePicker.SelectedDate = warranty.StartDate;
            EndDatePicker.SelectedDate = warranty.EndDate;
            VendorBox.Text = warranty.VendorName;
            TermsBox.Text = warranty.Terms;
        }
        else
        {
            Warranty = new WarrantyViewModel.WarrantyRow
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1)
            };
            TitleText.Text = "Add New Warranty";
            StartDatePicker.SelectedDate = Warranty.StartDate;
            EndDatePicker.SelectedDate = Warranty.EndDate;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var customers = await _apiService.GetAsync<List<CustomerLookupItem>>("api/customers");
            CustomerCombo.ItemsSource = customers;

            if (Warranty.CustomerId > 0)
            {
                CustomerCombo.SelectedValue = Warranty.CustomerId;
                await LoadAssetsForCustomerAsync(Warranty.CustomerId);
                AssetCombo.SelectedValue = Warranty.AssetId;
            }

            _isLoaded = true;
        }
        catch (Exception)
        {
            MessageBox.Show("Unable to load lookup data. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded && Warranty.WarrantyId > 0) return;

        if (CustomerCombo.SelectedValue is int customerId)
        {
            AssetCombo.ItemsSource = null;
            await LoadAssetsForCustomerAsync(customerId);
        }
    }

    private async System.Threading.Tasks.Task LoadAssetsForCustomerAsync(int customerId)
    {
        try
        {
            var assets = await _apiService.GetAsync<List<AssetLookupItem>>($"api/assets/by-customer/{customerId}");
            AssetCombo.ItemsSource = assets;
            if (assets != null && assets.Count > 0)
            {
                AssetCombo.SelectedIndex = 0;
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Unable to load assets. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CustomerCombo.SelectedValue is not int customerId)
        {
            MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (AssetCombo.SelectedValue is not int assetId)
        {
            MessageBox.Show("Please select an asset product.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Start and End dates are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Warranty.CustomerId = customerId;
        Warranty.AssetId = assetId;
        Warranty.StartDate = StartDatePicker.SelectedDate.Value;
        Warranty.EndDate = EndDatePicker.SelectedDate.Value;
        Warranty.VendorName = VendorBox.Text.Trim();
        Warranty.Terms = TermsBox.Text.Trim();

        var selectedCust = CustomerCombo.SelectedItem as CustomerLookupItem;
        Warranty.CustomerName = selectedCust?.CustomerName;

        var selectedAsset = AssetCombo.SelectedItem as AssetLookupItem;
        Warranty.AssetName = selectedAsset?.ProductName;
        Warranty.SerialNumber = selectedAsset?.SerialNumber;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public class CustomerLookupItem
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class AssetLookupItem
    {
        public int AssetId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
    }
}
