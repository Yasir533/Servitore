using System;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.ViewModels;

namespace Servitore.Desktop.Views.Dialogs;

public partial class UserEditDialog : Window
{
    public UserManagementViewModel.UserRow User { get; }
    public string? Password { get; private set; }

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
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var fullName = FullNameBox.Text.Trim();
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Full Name and Username are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (User.Id == 0 && string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Password is required for a new user.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (RoleCombo.SelectedItem is not ComboBoxItem roleItem)
        {
            MessageBox.Show("Please select a system role.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        User.FullName = fullName;
        User.Username = username;
        User.Email = EmailBox.Text.Trim();
        User.PhoneNumber = PhoneBox.Text.Trim();
        User.RoleName = roleItem.Tag?.ToString() ?? "Operator";
        User.IsActive = IsActiveCheck.IsChecked ?? true;
        Password = string.IsNullOrWhiteSpace(password) ? null : password;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
