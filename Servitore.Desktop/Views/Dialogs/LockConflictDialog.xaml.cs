using System;
using System.Windows;
using Servitore.Shared.Enums;

namespace Servitore.Desktop.Views.Dialogs;

public enum LockConflictResult
{
    Cancel,
    ViewOnly,
    TakeOver
}

public partial class LockConflictDialog : Window
{
    public LockConflictResult Result { get; private set; } = LockConflictResult.Cancel;

    public LockConflictDialog(string username, string lockTime, string computerName)
    {
        InitializeComponent();

        UserText.Text = username;
        TimeText.Text = lockTime;
        ComputerText.Text = string.IsNullOrEmpty(computerName) ? "Unknown Device" : computerName;

        // Take Over is restricted to Admins and Managers
        var currentUserRole = App.AuthenticationService.CurrentUser?.Role;
        if (currentUserRole == UserRole.Admin || currentUserRole == UserRole.Manager)
        {
            TakeOverBtn.Visibility = Visibility.Visible;
        }
        else
        {
            TakeOverBtn.Visibility = Visibility.Collapsed;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = LockConflictResult.Cancel;
        DialogResult = false;
        Close();
    }

    private void ViewOnly_Click(object sender, RoutedEventArgs e)
    {
        Result = LockConflictResult.ViewOnly;
        DialogResult = true;
        Close();
    }

    private void TakeOver_Click(object sender, RoutedEventArgs e)
    {
        Result = LockConflictResult.TakeOver;
        DialogResult = true;
        Close();
    }
}
