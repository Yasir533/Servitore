using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Views;

public partial class LiveUsersView : UserControl
{
    public LiveUsersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        App.SignalRService.UserPresenceListUpdated += OnPresenceUpdated;

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
                Dispatcher.Invoke(() =>
                {
                    UsersDataGrid.ItemsSource = list;
                });
            }
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to load user presence list", ex);
        }
    }

    private void OnPresenceUpdated(List<ConnectedUserDto> list)
    {
        Dispatcher.Invoke(() =>
        {
            UsersDataGrid.ItemsSource = list;
        });
    }
}
