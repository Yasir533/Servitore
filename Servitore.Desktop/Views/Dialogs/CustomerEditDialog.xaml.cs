using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class CustomerEditDialog : Window
{
    public CustomerViewModel.CustomerRow Customer { get; private set; }
    private readonly CustomerViewModel.CustomerRow _initialCustomer;
    private bool _isLoaded = false;
    private bool _isDirty = false;
    private bool _isClosingFromSave = false;
    private string _recordKey = string.Empty;
    private bool _isReadOnly = false;

    public CustomerEditDialog(CustomerViewModel.CustomerRow? customer = null)
    {
        InitializeComponent();
        
        if (customer != null)
        {
            Customer = customer;
            TitleText.Text = "Edit Customer Details";
            NameBox.Text = customer.CustomerName;
            CompanyBox.Text = customer.Company;
            MobileBox.Text = customer.Mobile;
            EmailBox.Text = customer.Email;
            AddressBox.Text = customer.Address;
            NotesBox.Text = customer.Notes;
        }
        else
        {
            Customer = new CustomerViewModel.CustomerRow();
            TitleText.Text = "Add New Customer";
        }

        // Deep clone for Reset functionality
        _initialCustomer = new CustomerViewModel.CustomerRow
        {
            CustomerId = Customer.CustomerId,
            CustomerName = Customer.CustomerName,
            Company = Customer.Company,
            Mobile = Customer.Mobile,
            Email = Customer.Email,
            Address = Customer.Address,
            Notes = Customer.Notes,
            ModifiedDate = Customer.ModifiedDate
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (Customer.CustomerId > 0)
        {
            _recordKey = $"Customer-{Customer.CustomerId}";
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
        _isLoaded = true;
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
        NameBox.IsEnabled = false;
        CompanyBox.IsEnabled = false;
        MobileBox.IsEnabled = false;
        EmailBox.IsEnabled = false;
        AddressBox.IsEnabled = false;
        NotesBox.IsEnabled = false;
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
        var name = NameBox.Text.Trim();
        var mobile = MobileBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationError("Customer Name is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(mobile))
        {
            ShowValidationError("Mobile number is required.");
            return false;
        }

        return true;
    }

    private void ApplyInputsToDto()
    {
        Customer.CustomerName = NameBox.Text.Trim();
        Customer.Company = CompanyBox.Text.Trim();
        Customer.Mobile = MobileBox.Text.Trim();
        Customer.Email = EmailBox.Text.Trim();
        Customer.Address = AddressBox.Text.Trim();
        Customer.Notes = NotesBox.Text.Trim();
    }

    private async Task<bool> SaveAsync()
    {
        if (_isReadOnly) return false;
        if (!ValidateInputs()) return false;

        var name = NameBox.Text.Trim();
        var mobile = MobileBox.Text.Trim();

        // Check for duplicates
        if (Customer.CustomerId == 0 || name != _initialCustomer.CustomerName || mobile != _initialCustomer.Mobile)
        {
            try
            {
                var check = await App.ApiService.GetAsync<DuplicateResponse>($"api/customers/check-duplicate?name={Uri.EscapeDataString(name)}&mobile={Uri.EscapeDataString(mobile)}");
                if (check != null && check.IsDuplicate)
                {
                    var proceed = DialogHelper.Confirm($"A customer with the name '{name}' and mobile '{mobile}' already exists. Do you want to save this as a duplicate?", "Duplicate Customer Detected");
                    if (!proceed)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Log("Failed checking duplicate customer.", ex);
            }
        }

        ApplyInputsToDto();

        if (Customer.CustomerId > 0)
        {
            try
            {
                var responseJson = await App.ApiService.PutAsync($"api/customers/{Customer.CustomerId}", Customer);
                var updated = System.Text.Json.JsonSerializer.Deserialize<CustomerViewModel.CustomerRow>(responseJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (updated != null)
                {
                    Customer.ModifiedDate = updated.ModifiedDate;
                }
            }
            catch (Services.ApiService.ConcurrencyException ex)
            {
                var serverCustomer = System.Text.Json.JsonSerializer.Deserialize<CustomerViewModel.CustomerRow>(ex.ServerJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (serverCustomer != null)
                {
                    var diffs = new List<ConflictDiff>();
                    if (Customer.CustomerName != serverCustomer.CustomerName)
                        diffs.Add(new ConflictDiff { FieldName = "Customer Name", LocalValue = Customer.CustomerName, ServerValue = serverCustomer.CustomerName });
                    if (Customer.Company != serverCustomer.Company)
                        diffs.Add(new ConflictDiff { FieldName = "Company", LocalValue = Customer.Company ?? "", ServerValue = serverCustomer.Company ?? "" });
                    if (Customer.Mobile != serverCustomer.Mobile)
                        diffs.Add(new ConflictDiff { FieldName = "Mobile", LocalValue = Customer.Mobile ?? "", ServerValue = serverCustomer.Mobile ?? "" });
                    if (Customer.Email != serverCustomer.Email)
                        diffs.Add(new ConflictDiff { FieldName = "Email", LocalValue = Customer.Email ?? "", ServerValue = serverCustomer.Email ?? "" });
                    if (Customer.Address != serverCustomer.Address)
                        diffs.Add(new ConflictDiff { FieldName = "Address", LocalValue = Customer.Address ?? "", ServerValue = serverCustomer.Address ?? "" });
                    if (Customer.Notes != serverCustomer.Notes)
                        diffs.Add(new ConflictDiff { FieldName = "Notes", LocalValue = Customer.Notes ?? "", ServerValue = serverCustomer.Notes ?? "" });

                    var resDialog = new ConflictResolutionDialog(diffs) { Owner = this };
                    resDialog.ShowDialog();

                    if (resDialog.Result == ConflictResolutionResult.KeepMine)
                    {
                        Customer.ModifiedDate = serverCustomer.ModifiedDate;
                        return await SaveAsync();
                    }
                    else if (resDialog.Result == ConflictResolutionResult.Reload)
                    {
                        Customer.ModifiedDate = serverCustomer.ModifiedDate;
                        NameBox.Text = serverCustomer.CustomerName;
                        CompanyBox.Text = serverCustomer.Company;
                        MobileBox.Text = serverCustomer.Mobile;
                        EmailBox.Text = serverCustomer.Email;
                        AddressBox.Text = serverCustomer.Address;
                        NotesBox.Text = serverCustomer.Notes;
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
                await App.ApiService.PostAsync<object, object>("api/customers", Customer);
                
                // Clear fields
                NameBox.Text = string.Empty;
                CompanyBox.Text = string.Empty;
                MobileBox.Text = string.Empty;
                EmailBox.Text = string.Empty;
                AddressBox.Text = string.Empty;
                NotesBox.Text = string.Empty;
                Customer = new CustomerViewModel.CustomerRow();
                _isDirty = false;
                ShowValidationError("Customer saved successfully. Ready for the next one!");
                
                // Green banner styling
                ErrorBanner.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
                ErrorBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 199, 132));
                ErrorBannerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowValidationError($"Unable to save customer: {ex.Message}");
            }
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly) return;
        _isLoaded = false;

        NameBox.Text = _initialCustomer.CustomerName;
        CompanyBox.Text = _initialCustomer.Company;
        MobileBox.Text = _initialCustomer.Mobile;
        EmailBox.Text = _initialCustomer.Email;
        AddressBox.Text = _initialCustomer.Address;
        NotesBox.Text = _initialCustomer.Notes;

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

    public class DuplicateResponse
    {
        public bool IsDuplicate { get; set; }
    }
}
