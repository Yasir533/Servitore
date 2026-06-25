using Microsoft.AspNetCore.SignalR.Client;
using Servitore.Shared.Constants;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Services;

// Maintains one live SignalR connection per desktop client so that when any
// other admin/engineer creates or updates a ticket, this screen is notified
// immediately and can refresh without polling the API.
public class SignalRService
{
    private HubConnection? _connection;

    public event Action<NotificationModel>? NotificationReceived;

    public async Task ConnectAsync(string apiBaseUrl, string token)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl.TrimEnd('/')}{AppConstants.NotificationHubUrl}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<NotificationModel>("ReceiveNotification", notification =>
        {
            NotificationReceived?.Invoke(notification);
        });

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
