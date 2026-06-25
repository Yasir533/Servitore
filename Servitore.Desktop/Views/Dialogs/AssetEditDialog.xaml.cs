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

    private string _recordKey = string.Empty;
    private bool _isReadOnly = false;

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

        if (Asset.AssetId > 0)
        {
            _recordKey = $"Asset-{Asset.AssetId}";
            var lockResult = await Helpers.LockHelper.AcquireLockAsync(_recordKey);
            if (!lockResult.Success)
            {
                var lockOwner = lockResult.Lock?.Username ?? "another user";
                var currentRole = App.AuthenticationService.CurrentUser?.Role.ToString() ?? "Operator";
                bool isAdminOrManager = currentRole == "Admin" || currentRole == "Manager";

                string msg = $"This record is currently being edited by {lockOwner}.\n\nClick Yes to View Only (Read-Only).";
                if (isAdminOrManager)
                {
                    msg += "\nClick No to Force Take Over editing rights.\nClick Cancel to go back.";
                }
                else
                {
                    msg += "\nClick Cancel to go back.";
                }

                MessageBoxResult action;
                if (isAdminOrManager)
                {
                    action = MessageBox.Show(this, msg, "Record Locked", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                }
                else
                {
                    action = MessageBox.Show(this, msg, "Record Locked", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (action == MessageBoxResult.OK) action = MessageBoxResult.Yes;
                }

                if (action == MessageBoxResult.Yes)
                {
                    _isReadOnly = true;
                    TitleText.Text += " (View Only)";
                    SaveButton.Visibility = Visibility.Collapsed;
                    DisableInputs();
                }
                else if (action == MessageBoxResult.No && isAdminOrManager)
                {
                    var takeover = await Helpers.LockHelper.TakeOverLockAsync(_recordKey);
                    if (!takeover.Success)
                    {
                        MessageBox.Show(this, "Failed to take over editing lock. Switching to View Only.", "Lock Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _isReadOnly = true;
                        TitleText.Text += " (View Only)";
                        SaveButton.Visibility = Visibility.Collapsed;
                        DisableInputs();
                    }
                    else
                    {
                        App.SignalRService.LockTakenOver += OnLockTakenOver;
                    }
                }
                else
                {
                    DialogResult = false;
                    Close();
                }
            }
            else
            {
                App.SignalRService.LockTakenOver += OnLockTakenOver;
            }
        }
    }

    private void OnLockTakenOver(string recordKey, string newOwner)
    {
        if (recordKey == _recordKey)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, $"Your editing session was taken over by {newOwner}. This window will now switch to View Only.", "Session Taken Over", MessageBoxButton.OK, MessageBoxImage.Warning);
                _isReadOnly = true;
                TitleText.Text = TitleText.Text.Replace("Details", "Details (View Only)");
                SaveButton.Visibility = Visibility.Collapsed;
                DisableInputs();
            });
        }
    }

    private void DisableInputs()
    {
        ProductNameBox.IsEnabled = false;
        AssetCodeBox.IsEnabled = false;
        SerialNumberBox.IsEnabled = false;
        CustomerCombo.IsEnabled = false;
        StatusCombo.IsEnabled = false;
        VendorNameBox.IsEnabled = false;
        PurchaseDatePicker.IsEnabled = false;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly) return;

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

        if (!string.IsNullOrEmpty(_recordKey))
        {
            await Helpers.LockHelper.ReleaseLockAsync(_recordKey);
        }

        DialogResult = true;
        Close();
    }

    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_recordKey) && !_isReadOnly)
        {
            await Helpers.LockHelper.ReleaseLockAsync(_recordKey);
        }
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        App.SignalRService.LockTakenOver -= OnLockTakenOver;
        base.OnClosed(e);
    }

    public class CustomerLookupItem
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }
}
