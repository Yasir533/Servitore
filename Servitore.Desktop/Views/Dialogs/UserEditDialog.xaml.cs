using System;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class UserEditDialog : Window
{
    public UserManagementViewModel.UserRow User { get; }
    public string? Password { get; private set; }

    private bool _isLoaded = false;
    private bool _isDirty = false;
    private bool _isClosingFromSave = false;

    public UserEditDialog(UserManagementViewModel.UserRow? user = null)
    {
        InitializeComponent();

        if (user != null)
        {
            User = user;
            TitleText.Text = "Edit User Details";
            FullNameBox.Text = user.FullName;
            UsernameBox.Text = user.Username;
            EmailBox.Text = user.Email;
            PhoneBox.Text = user.PhoneNumber;
            IsActiveCheck.IsChecked = user.IsActive;
            PasswordBox.Password = string.Empty; // Blank by default
        }
        else
        {
            User = new UserManagementViewModel.UserRow { IsActive = true };
            TitleText.Text = "Add New User";
            PasswordBox.Password = string.Empty;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FullNameBox.Focus();

        if (User.Id > 0 && !string.IsNullOrEmpty(User.RoleName))
        {
            foreach (ComboBoxItem item in RoleCombo.Items)
            {
                if (item.Tag?.ToString() == User.RoleName)
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }
        _isLoaded = true;
    }

    private void Input_Changed(object sender, EventArgs e)
    {
        if (_isLoaded)
        {
            _isDirty = true;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var fullName = FullNameBox.Text.Trim();
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
        {
            DialogHelper.ShowError("Full Name and Username are required.", "Validation Error");
            return;
        }

        if (User.Id == 0 && string.IsNullOrWhiteSpace(password))
        {
            DialogHelper.ShowError("Password is required for a new user.", "Validation Error");
            return;
        }

        if (RoleCombo.SelectedItem is not ComboBoxItem roleItem)
        {
            DialogHelper.ShowError("Please select a system role.", "Validation Error");
            return;
        }

        User.FullName = fullName;
        User.Username = username;
        User.Email = EmailBox.Text.Trim();
        User.PhoneNumber = PhoneBox.Text.Trim();
        User.RoleName = roleItem.Tag?.ToString() ?? "Operator";
        User.IsActive = IsActiveCheck.IsChecked ?? true;
        Password = string.IsNullOrWhiteSpace(password) ? null : password;

        _isClosingFromSave = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosingFromSave) return;

        if (_isDirty)
        {
            var result = DialogHelper.ConfirmSaveDiscardCancel("You have unsaved changes. Do you want to save them before closing?", "Unsaved Changes");
            if (result == ModernMessageResult.Save)
            {
                e.Cancel = true;
                Save_Click(sender, new RoutedEventArgs());
            }
            else if (result == ModernMessageResult.Discard)
            {
                DialogResult = false;
            }
            else
            {
                e.Cancel = true;
            }
        }
        else
        {
            DialogResult = false;
        }
    }
}
