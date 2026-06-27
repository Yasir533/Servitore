using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Servitore.Shared.Constants;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Services;

// Maintains one live SignalR connection per desktop client, mapping presence updates,
// record locks, data changes, and notifications in real time.
public class SignalRService
{
    private HubConnection? _connection;

    public event Action<NotificationModel>? NotificationReceived;
    public event Action<List<ConnectedUserDto>>? UserPresenceListUpdated;
    public event Action<DataEventModel>? DataChanged;
    public event Action<ActivityLogDto>? ActivityLogged;
    public event Action<string, string>? LockTakenOver; // (recordKey, newOwner)
    public event Action? LocksUpdated;
    public event Action? ForceLogoutReceived;
    public event Action<string?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    public string? ConnectionId => _connection?.ConnectionId;

    public async Task ConnectAsync(string apiBaseUrl, string token)
    {
        var machineName = Uri.EscapeDataString(Environment.MachineName);
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.0.0-enterprise";
        var hubUrl = $"{apiBaseUrl.TrimEnd('/')}{AppConstants.NotificationHubUrl}?computerName={machineName}&appVersion={version}";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.Reconnecting += ex =>
        {
            Reconnecting?.Invoke(ex?.Message);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Reconnected?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        _connection.Closed += ex =>
        {
            Closed?.Invoke(ex);
            return Task.CompletedTask;
        };

        _connection.On<NotificationModel>("ReceiveNotification", notification =>
        {
            NotificationReceived?.Invoke(notification);
        });

        _connection.On<List<ConnectedUserDto>>("UserPresenceListUpdated", list =>
        {
            UserPresenceListUpdated?.Invoke(list);
        });

        _connection.On<DataEventModel>("DataChanged", dataEvent =>
        {
            DataChanged?.Invoke(dataEvent);
        });

        _connection.On<ActivityLogDto>("ActivityLogged", log =>
        {
            ActivityLogged?.Invoke(log);
        });

        _connection.On<object>("LockTakenOver", rawObj =>
        {
            // SignalR dynamic object mapping helper
            try
            {
                var element = (System.Text.Json.JsonElement)rawObj;
                var recordKey = element.GetProperty("recordKey").GetString() ?? string.Empty;
                var newOwner = element.GetProperty("newOwner").GetString() ?? string.Empty;
                LockTakenOver?.Invoke(recordKey, newOwner);
            }
            catch (Exception)
            {
                // Fallback
            }
        });

        _connection.On("LocksUpdated", () =>
        {
            LocksUpdated?.Invoke();
        });

        _connection.On("OnForceLogout", () =>
        {
            ForceLogoutReceived?.Invoke();
        });

        await _connection.StartAsync();
    }

    public async Task UpdatePresenceAsync(string currentModule, string status)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.SendAsync("UpdatePresence", currentModule, status);
            }
            catch (Exception)
            {
                // Suppress background trace failures
            }
        }
    }

    public async Task BroadcastDataChangeAsync(DataEventModel dataEvent)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.SendAsync("BroadcastDataChange", dataEvent);
            }
            catch (Exception)
            {
                // Suppress background trace failures
            }
        }
    }

    public async Task ForceLogoutAsync(string connectionId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.SendAsync("ForceLogout", connectionId);
            }
            catch (Exception) { }
        }
    }

    public async Task SendBroadcastAsync(string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.SendAsync("SendBroadcast", message);
            }
            catch (Exception) { }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
