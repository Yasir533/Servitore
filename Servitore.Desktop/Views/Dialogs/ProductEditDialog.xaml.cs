using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class ProductEditDialog : Window
{
    private readonly ApiService _apiService;
    public ProductViewModel.ProductRow Product { get; private set; }
    private readonly ProductViewModel.ProductRow _initialProduct;
    private bool _isLoaded = false;
    private bool _isDirty = false;
    private bool _isClosingFromSave = false;
    private string _recordKey = string.Empty;
    private bool _isReadOnly = false;

    public ProductEditDialog(ApiService apiService, ProductViewModel.ProductRow? product = null)
    {
        InitializeComponent();
        _apiService = apiService;

        if (product != null)
        {
            Product = product;
            TitleText.Text = "Edit Product Details";
            ProductNameBox.Text = product.ProductName;
            AssetCodeBox.Text = product.ProductCode;
            SerialNumberBox.Text = product.SerialNumber;
            VendorNameBox.Text = product.VendorName;
            PurchaseDatePicker.SelectedDate = product.PurchaseDate;
        }
        else
        {
            Product = new ProductViewModel.ProductRow();
            TitleText.Text = "Add New Product";
        }

        // Deep clone for Reset functionality
        _initialProduct = new ProductViewModel.ProductRow
        {
            ProductId = Product.ProductId,
            ProductCode = Product.ProductCode,
            ProductName = Product.ProductName,
            SerialNumber = Product.SerialNumber,
            CustomerId = Product.CustomerId,
            CustomerName = Product.CustomerName,
            Status = Product.Status,
            VendorName = Product.VendorName,
            PurchaseDate = Product.PurchaseDate,
            ModifiedDate = Product.ModifiedDate
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var customers = await _apiService.GetAsync<List<CustomerLookupItem>>("api/customers");
            CustomerCombo.ItemsSource = customers;

            if (Product.CustomerId > 0)
            {
                CustomerCombo.SelectedValue = Product.CustomerId;
            }

            if (!string.IsNullOrEmpty(Product.Status))
            {
                foreach (System.Windows.Controls.ComboBoxItem item in StatusCombo.Items)
                {
                    if (item.Tag?.ToString() == Product.Status)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }

            _isLoaded = true;
        }
        catch (Exception)
        {
            ShowValidationError("Unable to load lookup data. Please try again.");
        }

        if (Product.ProductId > 0)
        {
            _recordKey = $"Asset-{Product.ProductId}";
            var lockResult = await LockHelper.AcquireLockAsync(_recordKey);
            if (!lockResult.Success)
            {
                var lockOwner = lockResult.Lock?.Username ?? "another user";
                var lockTime = lockResult.Lock?.LockedAt.ToLocalTime().ToString("t") ?? "Unknown Time";
                var computer = lockResult.Lock?.ComputerName ?? "Unknown Device";

                var conflictDialog = new LockConflictDialog(lockOwner, lockTime, computer)
                {
                    Owner = this
                };

                conflictDialog.ShowDialog();

                if (conflictDialog.Result == LockConflictResult.ViewOnly)
                {
                    _isReadOnly = true;
                    TitleText.Text += " (View Only)";
                    SaveButton.Visibility = Visibility.Collapsed;
                    SaveNewButton.Visibility = Visibility.Collapsed;
                    DisableInputs();
                }
                else if (conflictDialog.Result == LockConflictResult.TakeOver)
                {
                    var takeover = await LockHelper.TakeOverLockAsync(_recordKey);
                    if (!takeover.Success)
                    {
                        DialogHelper.ShowError("Failed to take over editing lock. Switching to View Only.", "Lock Conflict");
                        _isReadOnly = true;
                        TitleText.Text += " (View Only)";
                        SaveButton.Visibility = Visibility.Collapsed;
                        SaveNewButton.Visibility = Visibility.Collapsed;
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
                DialogHelper.ShowError($"Your editing session was taken over by {newOwner}. This window will now switch to View Only.", "Session Taken Over");
                _isReadOnly = true;
                TitleText.Text = TitleText.Text.Replace("Details", "Details (View Only)");
                SaveButton.Visibility = Visibility.Collapsed;
                SaveNewButton.Visibility = Visibility.Collapsed;
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

    private void Input_Changed(object sender, EventArgs e)
    {
        if (_isLoaded)
        {
            _isDirty = true;
            ClearValidationError();
        }
    }

    private void ShowValidationError(string message)
    {
        ErrorBannerText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void ClearValidationError()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
    }

    private bool ValidateInputs()
    {
        ClearValidationError();
        var name = ProductNameBox.Text.Trim();
        var code = AssetCodeBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationError("Product Name is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            ShowValidationError("Product Code is required.");
            return false;
        }

        if (CustomerCombo.SelectedValue is not int)
        {
            ShowValidationError("Please select a customer.");
            return false;
        }

        return true;
    }

    private void ApplyInputsToDto()
    {
        Product.ProductName = ProductNameBox.Text.Trim();
        Product.ProductCode = AssetCodeBox.Text.Trim();
        Product.SerialNumber = SerialNumberBox.Text.Trim();
        Product.CustomerId = (int)CustomerCombo.SelectedValue;
        
        var selectedCust = CustomerCombo.SelectedItem as CustomerLookupItem;
        Product.CustomerName = selectedCust?.CustomerName;

        var selectedStatusItem = StatusCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
        Product.Status = selectedStatusItem?.Tag?.ToString() ?? "Active";
        Product.VendorName = VendorNameBox.Text.Trim();
        Product.PurchaseDate = PurchaseDatePicker.SelectedDate;
    }

    private async Task<bool> SaveAsync()
    {
        if (_isReadOnly) return false;
        if (!ValidateInputs()) return false;

        ApplyInputsToDto();

        if (Product.ProductId > 0)
        {
            try
            {
                // Create a matching DTO for serialization to API
                var apiDto = new
                {
                    AssetId = Product.ProductId,
                    AssetCode = Product.ProductCode,
                    ProductName = Product.ProductName,
                    SerialNumber = Product.SerialNumber,
                    CustomerId = Product.CustomerId,
                    Status = Product.Status == "Active" ? 1 : Product.Status == "Inactive" ? 2 : Product.Status == "Scrapped" ? 3 : 4, // AssetStatus enum
                    VendorName = Product.VendorName,
                    PurchaseDate = Product.PurchaseDate,
                    ModifiedDate = Product.ModifiedDate
                };

                var responseJson = await _apiService.PutAsync($"api/assets/{Product.ProductId}", apiDto);
                var updated = System.Text.Json.JsonSerializer.Deserialize<ProductViewModel.ProductRow>(responseJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (updated != null)
                {
                    Product.ModifiedDate = updated.ModifiedDate;
                }
            }
            catch (Services.ApiService.ConcurrencyException ex)
            {
                var serverAsset = System.Text.Json.JsonSerializer.Deserialize<ProductViewModel.ProductRow>(ex.ServerJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (serverAsset != null)
                {
                    var diffs = new List<ConflictDiff>();
                    if (Product.ProductName != serverAsset.ProductName)
                        diffs.Add(new ConflictDiff { FieldName = "Product Name", LocalValue = Product.ProductName, ServerValue = serverAsset.ProductName });
                    if (Product.ProductCode != serverAsset.ProductCode)
                        diffs.Add(new ConflictDiff { FieldName = "Product Code", LocalValue = Product.ProductCode, ServerValue = serverAsset.ProductCode });
                    if (Product.SerialNumber != serverAsset.SerialNumber)
                        diffs.Add(new ConflictDiff { FieldName = "Serial Number", LocalValue = Product.SerialNumber ?? "", ServerValue = serverAsset.SerialNumber ?? "" });
                    if (Product.VendorName != serverAsset.VendorName)
                        diffs.Add(new ConflictDiff { FieldName = "Vendor Name", LocalValue = Product.VendorName ?? "", ServerValue = serverAsset.VendorName ?? "" });
                    if (Product.PurchaseDate != serverAsset.PurchaseDate)
                        diffs.Add(new ConflictDiff { FieldName = "Purchase Date", LocalValue = Product.PurchaseDate?.ToString("d") ?? "", ServerValue = serverAsset.PurchaseDate?.ToString("d") ?? "" });
                    if (Product.Status != serverAsset.Status)
                        diffs.Add(new ConflictDiff { FieldName = "Status", LocalValue = Product.Status, ServerValue = serverAsset.Status });
                    if (Product.CustomerId != serverAsset.CustomerId)
                        diffs.Add(new ConflictDiff { FieldName = "Customer ID", LocalValue = Product.CustomerId.ToString(), ServerValue = serverAsset.CustomerId.ToString() });

                    var resDialog = new ConflictResolutionDialog(diffs) { Owner = this };
                    resDialog.ShowDialog();

                    if (resDialog.Result == ConflictResolutionResult.KeepMine)
                    {
                        Product.ModifiedDate = serverAsset.ModifiedDate;
                        return await SaveAsync();
                    }
                    else if (resDialog.Result == ConflictResolutionResult.Reload)
                    {
                        Product.ModifiedDate = serverAsset.ModifiedDate;
                        ProductNameBox.Text = serverAsset.ProductName;
                        AssetCodeBox.Text = serverAsset.ProductCode;
                        SerialNumberBox.Text = serverAsset.SerialNumber;
                        CustomerCombo.SelectedValue = serverAsset.CustomerId;
                        VendorNameBox.Text = serverAsset.VendorName;
                        PurchaseDatePicker.SelectedDate = serverAsset.PurchaseDate;
                        
                        foreach (System.Windows.Controls.ComboBoxItem item in StatusCombo.Items)
                        {
                            if (item.Tag?.ToString() == serverAsset.Status)
                            {
                                item.IsSelected = true;
                                break;
                            }
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowValidationError($"Unable to save changes: {ex.Message}");
                return false;
            }
        }

        if (!string.IsNullOrEmpty(_recordKey))
        {
            await LockHelper.ReleaseLockAsync(_recordKey);
            _recordKey = string.Empty;
        }

        _isDirty = false;
        return true;
    }

    private async void SaveClose_Click(object sender, RoutedEventArgs e)
    {
        if (await SaveAsync())
        {
            _isClosingFromSave = true;
            DialogResult = true;
            Close();
        }
    }

    private async void SaveNew_Click(object sender, RoutedEventArgs e)
    {
        if (await SaveAsync())
        {
            try
            {
                var apiDto = new
                {
                    AssetCode = Product.ProductCode,
                    ProductName = Product.ProductName,
                    SerialNumber = Product.SerialNumber,
                    CustomerId = Product.CustomerId,
                    Status = Product.Status == "Active" ? 1 : Product.Status == "Inactive" ? 2 : Product.Status == "Scrapped" ? 3 : 4,
                    VendorName = Product.VendorName,
                    PurchaseDate = Product.PurchaseDate
                };

                await _apiService.PostAsync<object, object>("api/assets", apiDto);
                
                // Clear fields
                ProductNameBox.Text = string.Empty;
                AssetCodeBox.Text = string.Empty;
                SerialNumberBox.Text = string.Empty;
                VendorNameBox.Text = string.Empty;
                PurchaseDatePicker.SelectedDate = null;
                Product = new ProductViewModel.ProductRow();
                _isDirty = false;
                ShowValidationError("Product saved successfully. Ready for the next one!");
                
                // Green banner styling
                ErrorBanner.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
                ErrorBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 199, 132));
                ErrorBannerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowValidationError($"Unable to save product: {ex.Message}");
            }
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly) return;
        _isLoaded = false;

        ProductNameBox.Text = _initialProduct.ProductName;
        AssetCodeBox.Text = _initialProduct.ProductCode;
        SerialNumberBox.Text = _initialProduct.SerialNumber;
        CustomerCombo.SelectedValue = _initialProduct.CustomerId;
        VendorNameBox.Text = _initialProduct.VendorName;
        PurchaseDatePicker.SelectedDate = _initialProduct.PurchaseDate;

        foreach (System.Windows.Controls.ComboBoxItem item in StatusCombo.Items)
        {
            if (item.Tag?.ToString() == _initialProduct.Status)
            {
                item.IsSelected = true;
                break;
            }
        }

        _isDirty = false;
        ClearValidationError();
        _isLoaded = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosingFromSave) return;

        if (_isDirty)
        {
            var result = DialogHelper.ConfirmSaveDiscardCancel("You have unsaved changes. Do you want to save them before closing?", "Unsaved Changes");
            if (result == ModernMessageResult.Save)
            {
                e.Cancel = true;
                if (await SaveAsync())
                {
                    _isClosingFromSave = true;
                    DialogResult = true;
                    Close();
                }
            }
            else if (result == ModernMessageResult.Discard)
            {
                if (!string.IsNullOrEmpty(_recordKey) && !_isReadOnly)
                {
                    await LockHelper.ReleaseLockAsync(_recordKey);
                }
            }
            else
            {
                e.Cancel = true;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_recordKey) && !_isReadOnly)
            {
                await LockHelper.ReleaseLockAsync(_recordKey);
            }
        }
    }

    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.S && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            e.Handled = true;
            if (await SaveAsync())
            {
                _isClosingFromSave = true;
                DialogResult = true;
                Close();
            }
        }
        else if (e.Key == System.Windows.Input.Key.N && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            e.Handled = true;
            SaveNew_Click(sender, new RoutedEventArgs());
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            e.Handled = true;
            Close();
        }
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
