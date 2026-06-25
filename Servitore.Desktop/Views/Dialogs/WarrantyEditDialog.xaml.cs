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

    private string _recordKey = string.Empty;
    private bool _isReadOnly = false;

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

        if (Warranty.WarrantyId > 0)
        {
            _recordKey = $"Warranty-{Warranty.WarrantyId}";
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
        CustomerCombo.IsEnabled = false;
        AssetCombo.IsEnabled = false;
        StartDatePicker.IsEnabled = false;
        EndDatePicker.IsEnabled = false;
        VendorBox.IsEnabled = false;
        TermsBox.IsEnabled = false;
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

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly) return;

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

    public class AssetLookupItem
    {
        public int AssetId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
    }
}
