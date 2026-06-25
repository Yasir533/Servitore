using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Services;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class AMCEditDialog : Window
{
    private readonly ApiService _apiService;
    public AMCViewModel.AMCRow Contract { get; }
    private bool _isLoaded = false;

    public AMCEditDialog(ApiService apiService, AMCViewModel.AMCRow? contract = null)
    {
        InitializeComponent();
        _apiService = apiService;

        if (contract != null)
        {
            Contract = contract;
            TitleText.Text = "Edit AMC Contract";
            StartDatePicker.SelectedDate = contract.StartDate;
            EndDatePicker.SelectedDate = contract.EndDate;
            ValueBox.Text = contract.ContractValue.ToString("0");
            VisitsBox.Text = contract.VisitsIncluded.ToString();
        }
        else
        {
            Contract = new AMCViewModel.AMCRow
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1)
            };
            TitleText.Text = "Add New AMC Contract";
            StartDatePicker.SelectedDate = Contract.StartDate;
            EndDatePicker.SelectedDate = Contract.EndDate;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var customers = await _apiService.GetAsync<List<CustomerLookupItem>>("api/customers");
            CustomerCombo.ItemsSource = customers;

            if (Contract.CustomerId > 0)
            {
                CustomerCombo.SelectedValue = Contract.CustomerId;
                await LoadAssetsForCustomerAsync(Contract.CustomerId);
                AssetCombo.SelectedValue = Contract.AssetId;
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load lookup data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded && Contract.AMCContractId > 0) return;

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
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load assets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        if (!decimal.TryParse(ValueBox.Text.Trim(), out var contractValue) || contractValue < 0)
        {
            MessageBox.Show("Contract Value must be a valid positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(VisitsBox.Text.Trim(), out var visits) || visits < 0)
        {
            MessageBox.Show("Visits Included must be a valid positive integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Contract.CustomerId = customerId;
        Contract.AssetId = assetId;
        Contract.StartDate = StartDatePicker.SelectedDate.Value;
        Contract.EndDate = EndDatePicker.SelectedDate.Value;
        Contract.ContractValue = contractValue;
        Contract.VisitsIncluded = visits;

        var selectedCust = CustomerCombo.SelectedItem as CustomerLookupItem;
        Contract.CustomerName = selectedCust?.CustomerName;

        var selectedAsset = AssetCombo.SelectedItem as AssetLookupItem;
        Contract.AssetName = selectedAsset?.ProductName;

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
    }
}
