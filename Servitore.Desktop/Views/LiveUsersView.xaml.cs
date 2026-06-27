using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Servitore.Shared.Models;
using Servitore.Desktop.Helpers;

namespace Servitore.Desktop.Views;

public partial class LiveUsersView : UserControl
{
    public LiveUsersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        App.SignalRService.UserPresenceListUpdated -= OnPresenceUpdated;
        App.SignalRService.UserPresenceListUpdated += OnPresenceUpdated;

        // Apply admin/manager visibility constraints
        var currentRole = App.AuthenticationService.CurrentUser?.Role;
        bool isAdminOrManager = currentRole == Servitore.Shared.Enums.UserRole.Admin || currentRole == Servitore.Shared.Enums.UserRole.Manager;

        AppVersionColumn.Visibility = isAdminOrManager ? Visibility.Visible : Visibility.Collapsed;
        EditingRecordColumn.Visibility = isAdminOrManager ? Visibility.Visible : Visibility.Collapsed;
        ActionsColumn.Visibility = isAdminOrManager ? Visibility.Visible : Visibility.Collapsed;
        BroadcastBtn.Visibility = isAdminOrManager ? Visibility.Visible : Visibility.Collapsed;

        await LoadPresenceListAsync();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        App.SignalRService.UserPresenceListUpdated -= OnPresenceUpdated;
    }

    private async Task LoadPresenceListAsync()
    {
        try
        {
            var list = await App.ApiService.GetAsync<List<ConnectedUserDto>>("api/auth/presence");
            if (list != null)
            {
                UpdateUI(list);
            }
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to load user presence list", ex);
        }
    }

    private void OnPresenceUpdated(List<ConnectedUserDto> list)
    {
        UpdateUI(list);
    }

    private void UpdateUI(List<ConnectedUserDto> list)
    {
        Dispatcher.Invoke(() =>
        {
            UsersDataGrid.ItemsSource = list;

            // Update KPI metric counts
            TotalUsersText.Text = list.Count.ToString();
            ActiveUsersText.Text = list.Count(u => u.Status == "Online").ToString();
            IdleUsersText.Text = list.Count(u => u.Status == "Busy").ToString();
        });
    }

    private void BroadcastBtn_Click(object sender, RoutedEventArgs e)
    {
        // Custom simple inline broadcast window
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(new TextBlock { Text = "Enter message to broadcast to all logged in users:", Margin = new Thickness(0,0,0,8), FontWeight = FontWeights.SemiBold });
        
        var txtInput = new TextBox { Height = 32, VerticalContentAlignment = VerticalAlignment.Center, Focusable = true };
        stack.Children.Add(txtInput);

        var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,16,0,0) };
        var cancelBtn = new Button { Content = "Cancel", Style = (Style)FindResource("MaterialDesignFlatButton"), IsCancel = true, Margin = new Thickness(0,0,8,0) };
        var sendBtn = new Button { Content = "Send", Style = (Style)FindResource("MaterialDesignRaisedButton"), IsDefault = true };
        
        buttonStack.Children.Add(cancelBtn);
        buttonStack.Children.Add(sendBtn);
        stack.Children.Add(buttonStack);

        var dialog = new Window
        {
            Title = "Broadcast Message",
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ResizeMode = ResizeMode.NoResize,
            Background = (System.Windows.Media.Brush)FindResource("MaterialDesignPaper"),
            Content = stack
        };

        sendBtn.Click += async (s, ev) =>
        {
            var msg = txtInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(msg))
            {
                await App.SignalRService.SendBroadcastAsync(msg);
                dialog.DialogResult = true;
                dialog.Close();
            }
        };

        dialog.ShowDialog();
    }

    private void SessionDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ConnectedUserDto user)
        {
            var details = $"Username: {user.Username}\n" +
                          $"Role: {user.Role}\n" +
                          $"Computer Name: {user.ComputerName}\n" +
                          $"IP Address: {user.IpAddress}\n" +
                          $"App Version: {user.AppVersion}\n" +
                          $"Current Screen: {user.CurrentModule}\n" +
                          $"Currently Editing: {user.EditingRecord ?? "None"}\n" +
                          $"Login Time: {user.LoginTime.ToLocalTime():dd MMM yyyy HH:mm:ss}\n" +
                          $"Last Activity: {user.LastActivity.ToLocalTime():HH:mm:ss}\n" +
                          $"Connection ID: {user.ConnectionId}";

            DialogHelper.ShowInfo(details, "User Session Details");
        }
    }

    private async void ForceLogout_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ConnectedUserDto user)
        {
            if (user.ConnectionId == App.SignalRService.ConnectionId)
            {
                DialogHelper.ShowError("You cannot force log out yourself.", "Action Prohibited");
                return;
            }

            var confirm = DialogHelper.Confirm(
                $"Are you sure you want to force logout user '{user.Username}'?", 
                "Force User Logout");

            if (confirm)
            {
                await App.SignalRService.ForceLogoutAsync(user.ConnectionId);
                DialogHelper.ShowInfo($"Forced logout signal sent to connection {user.ConnectionId}.", "Logout Signal Sent");
            }
        }
    }
}
