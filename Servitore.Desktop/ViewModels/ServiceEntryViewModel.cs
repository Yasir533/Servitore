using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class ServiceEntryViewModel : ViewModelBase, IDisposable
{
    private readonly ApiService _apiService;
    private readonly SignalRService _signalRService;
    private readonly ObservableCollection<ServiceEntryRow> _allEntries = new();

    public ICollectionView ServiceEntriesView { get; }

    public List<string> StatusFilters { get; } =
        new() { "All", "Open", "In Progress", "On Hold", "Resolved", "Closed" };

    [ObservableProperty]
    private bool isLoading;

    private string _selectedStatusFilter = "All";
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set { SetProperty(ref _selectedStatusFilter, value); ServiceEntriesView.Refresh(); }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ServiceEntriesView.Refresh(); }
    }

    public ServiceEntryViewModel(ApiService apiService, SignalRService signalRService)
    {
        _apiService = apiService;
        _signalRService = signalRService;
        ServiceEntriesView = CollectionViewSource.GetDefaultView(_allEntries);
        ServiceEntriesView.Filter = FilterServiceEntry;
        _signalRService.NotificationReceived += OnNotificationReceived;
        _signalRService.DataChanged += OnDataChanged;
    }

    private async void OnNotificationReceived(NotificationModel notification)
    {
        await LoadAsync();
    }

    private async void OnDataChanged(Servitore.Shared.Models.DataEventModel dataEvent)
    {
        if (dataEvent.EntityType == "ServiceEntry")
        {
            await LoadAsync();
        }
    }

    private bool FilterServiceEntry(object obj)
    {
        if (obj is not ServiceEntryRow t) return false;
        if (SelectedStatusFilter != "All" && t.Status != SelectedStatusFilter) return false;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.ToLower();
            if (!t.ServiceEntryNumber.ToLower().Contains(q) &&
                !(t.CustomerName?.ToLower().Contains(q) ?? false))
                return false;
        }
        return true;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _apiService.GetAsync<List<ServiceEntryRow>>("api/serviceentries");
            _allEntries.Clear();
            if (results is not null)
            {
                foreach (var t in results) _allEntries.Add(t);
            }
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to load service entry data", ex);
            Helpers.ToastHelper.ShowToast("Failed to load service entries.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Dispose()
    {
        _signalRService.NotificationReceived -= OnNotificationReceived;
        _signalRService.DataChanged -= OnDataChanged;
    }

    [RelayCommand]
    private async Task NewServiceEntry()
    {
        var dialog = new Views.Dialogs.ServiceEntryEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                await LoadAsync();

                await _signalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
                {
                    EntityType = "ServiceEntry",
                    Action = "Created",
                    RecordId = "New",
                    DisplayName = $"Service Entry for {dialog.ServiceEntry.CustomerName}",
                    Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
                });
            }
            catch (Exception)
            {
                Helpers.DialogHelper.ShowError("Unable to refresh service entries.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task ViewServiceEntry(ServiceEntryRow? row)
    {
        if (row is null) return;

        IsLoading = true;
        try
        {
            var entryDetails = await _apiService.GetAsync<ServiceEntryDetailsDto>($"api/serviceentries/{row.ServiceEntryId}");
            if (entryDetails == null) return;

            var dialog = new Views.Dialogs.ServiceEntryEditDialog(_apiService, entryDetails)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                await LoadAsync();

                await _signalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
                {
                    EntityType = "ServiceEntry",
                    Action = "Updated",
                    RecordId = row.ServiceEntryId.ToString(),
                    DisplayName = row.ServiceEntryNumber,
                    Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
                });
            }
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to load or refresh service entry details.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CloseServiceEntry(ServiceEntryRow? row)
    {
        if (row is null) return;
        try
        {
            await _apiService.PutAsync($"api/serviceentries/{row.ServiceEntryId}/close", new { });
            await LoadAsync();

            await _signalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
            {
                EntityType = "ServiceEntry",
                Action = "Closed",
                RecordId = row.ServiceEntryId.ToString(),
                DisplayName = row.ServiceEntryNumber,
                Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
            });
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to close service entry. Please try again later.");
        }
    }

    public class ServiceEntryRow
    {
        public int ServiceEntryId { get; set; }
        public string ServiceEntryNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? AssetName { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
