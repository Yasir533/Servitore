using System;
using System.Collections.Generic;
using System.Windows;
using Servitore.Desktop.Services;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class AssetEditDialog : Window
{
    private readonly ApiService _apiService;
    public AssetViewModel.AssetRow Asset { get; }

    public AssetEditDialog(ApiService apiService, AssetViewModel.AssetRow? asset = null)
    {
        InitializeComponent();
        _apiService = apiService;

        if (asset != null)
        {
            Asset = asset;
            TitleText.Text = "Edit Asset Details";
            ProductNameBox.Text = asset.ProductName;
            AssetCodeBox.Text = asset.AssetCode;
            SerialNumberBox.Text = asset.SerialNumber;
            VendorNameBox.Text = asset.VendorName;
            PurchaseDatePicker.SelectedDate = asset.PurchaseDate;
        }
        else
        {
            Asset = new AssetViewModel.AssetRow();
            TitleText.Text = "Add New Asset";
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var customers = await _apiService.GetAsync<List<CustomerLookupItem>>("api/customers");
            CustomerCombo.ItemsSource = customers;

            if (Asset.CustomerId > 0)
            {
                CustomerCombo.SelectedValue = Asset.CustomerId;
            }

            if (!string.IsNullOrEmpty(Asset.Status))
            {
                foreach (System.Windows.Controls.ComboBoxItem item in StatusCombo.Items)
                {
                    if (item.Tag?.ToString() == Asset.Status)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Unable to load lookup data. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var product = ProductNameBox.Text.Trim();
        var code = AssetCodeBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(product) || string.IsNullOrWhiteSpace(code))
        {
            MessageBox.Show("Product Name and Asset Code are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CustomerCombo.SelectedValue is not int customerId)
        {
            MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Asset.ProductName = product;
        Asset.AssetCode = code;
        Asset.SerialNumber = SerialNumberBox.Text.Trim();
        Asset.CustomerId = customerId;
        
        var selectedCust = CustomerCombo.SelectedItem as CustomerLookupItem;
        Asset.CustomerName = selectedCust?.CustomerName;

        var selectedStatusItem = StatusCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
        Asset.Status = selectedStatusItem?.Tag?.ToString() ?? "Active";
        Asset.VendorName = VendorNameBox.Text.Trim();
        Asset.PurchaseDate = PurchaseDatePicker.SelectedDate;

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
}
