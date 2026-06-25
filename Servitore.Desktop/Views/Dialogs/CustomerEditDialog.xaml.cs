using System;
using System.Windows;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class CustomerEditDialog : Window
{
    public CustomerViewModel.CustomerRow Customer { get; }
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
            ContactBox.Text = customer.ContactPerson;
            MobileBox.Text = customer.Mobile;
            EmailBox.Text = customer.Email;
            AddressBox.Text = customer.Address;
        }
        else
        {
            Customer = new CustomerViewModel.CustomerRow();
            TitleText.Text = "Add New Customer";
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (Customer.CustomerId > 0)
        {
            _recordKey = $"Customer-{Customer.CustomerId}";
            var lockResult = await Helpers.LockHelper.AcquireLockAsync(_recordKey);
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
                    DisableInputs();
                }
                else if (conflictDialog.Result == LockConflictResult.TakeOver)
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
        NameBox.IsEnabled = false;
        ContactBox.IsEnabled = false;
        MobileBox.IsEnabled = false;
        EmailBox.IsEnabled = false;
        AddressBox.IsEnabled = false;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly) return;

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Customer.CustomerName = name;
        Customer.ContactPerson = ContactBox.Text.Trim();
        Customer.Mobile = MobileBox.Text.Trim();
        Customer.Email = EmailBox.Text.Trim();
        Customer.Address = AddressBox.Text.Trim();

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
                    var diffs = new System.Collections.Generic.List<ConflictDiff>();
                    if (Customer.CustomerName != serverCustomer.CustomerName)
                        diffs.Add(new ConflictDiff { FieldName = "Customer Name", LocalValue = Customer.CustomerName, ServerValue = serverCustomer.CustomerName });
                    if (Customer.ContactPerson != serverCustomer.ContactPerson)
                        diffs.Add(new ConflictDiff { FieldName = "Contact Person", LocalValue = Customer.ContactPerson ?? "", ServerValue = serverCustomer.ContactPerson ?? "" });
                    if (Customer.Mobile != serverCustomer.Mobile)
                        diffs.Add(new ConflictDiff { FieldName = "Mobile", LocalValue = Customer.Mobile ?? "", ServerValue = serverCustomer.Mobile ?? "" });
                    if (Customer.Email != serverCustomer.Email)
                        diffs.Add(new ConflictDiff { FieldName = "Email", LocalValue = Customer.Email ?? "", ServerValue = serverCustomer.Email ?? "" });
                    if (Customer.Address != serverCustomer.Address)
                        diffs.Add(new ConflictDiff { FieldName = "Address", LocalValue = Customer.Address ?? "", ServerValue = serverCustomer.Address ?? "" });

                    var resDialog = new ConflictResolutionDialog(diffs) { Owner = this };
                    resDialog.ShowDialog();

                    if (resDialog.Result == ConflictResolutionResult.KeepMine)
                    {
                        Customer.ModifiedDate = serverCustomer.ModifiedDate;
                        Save_Click(sender, e);
                        return;
                    }
                    else if (resDialog.Result == ConflictResolutionResult.Reload)
                    {
                        Customer.ModifiedDate = serverCustomer.ModifiedDate;
                        NameBox.Text = serverCustomer.CustomerName;
                        ContactBox.Text = serverCustomer.ContactPerson;
                        MobileBox.Text = serverCustomer.Mobile;
                        EmailBox.Text = serverCustomer.Email;
                        AddressBox.Text = serverCustomer.Address;
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to save changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

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
}
