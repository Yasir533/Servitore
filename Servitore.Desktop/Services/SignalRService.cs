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

    private int _busyRefCount = 0;
    private string _currentStatus = "Offline";
    private string _currentModule = "Dashboard";

    public event Action<string>? CurrentStatusChanged;

    public string CurrentStatus
    {
        get => _currentStatus;
        private set
        {
            if (_currentStatus != value)
            {
                _currentStatus = value;
                CurrentStatusChanged?.Invoke(value);
                _ = UpdatePresenceAsync(_currentModule, value);
            }
        }
    }

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
            EvaluateStatus();
            Reconnecting?.Invoke(ex?.Message);
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            EvaluateStatus();
            try
            {
                await _connection.SendAsync("LogConnectionRestored");
            }
            catch (Exception) { }
            Reconnected?.Invoke(connectionId);
        };

        _connection.Closed += ex =>
        {
            EvaluateStatus();
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
        EvaluateStatus();
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
        EvaluateStatus();
    }

    public void UpdateCurrentModule(string module)
    {
        _currentModule = module;
        _ = UpdatePresenceAsync(module, CurrentStatus);
    }

    public void IncrementBusy()
    {
        System.Threading.Interlocked.Increment(ref _busyRefCount);
        EvaluateStatus();
    }

    public void DecrementBusy()
    {
        if (System.Threading.Interlocked.Decrement(ref _busyRefCount) < 0)
        {
            System.Threading.Interlocked.Exchange(ref _busyRefCount, 0);
        }
        EvaluateStatus();
    }

    public IDisposable GetBusyScope()
    {
        return new BusyScope(this);
    }

    private void EvaluateStatus()
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            CurrentStatus = "Offline";
        }
        else if (_busyRefCount > 0)
        {
            CurrentStatus = "Busy";
        }
        else
        {
            CurrentStatus = "Online";
        }
    }

    private class BusyScope : IDisposable
    {
        private readonly SignalRService _service;
        private bool _disposed;

        public BusyScope(SignalRService service)
        {
            _service = service;
            _service.IncrementBusy();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _service.DecrementBusy();
                _disposed = true;
            }
        }
    }
}
