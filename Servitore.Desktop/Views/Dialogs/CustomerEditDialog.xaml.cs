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
