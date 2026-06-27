using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Servitore.Desktop.ViewModels;

public partial class RecentlyDeletedViewModel : ViewModelBase, IDisposable
{
    private readonly ApiService _apiService;
    private readonly SignalRService _signalRService;
    private readonly List<RecentlyDeletedItem> _allItems = new();

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string searchText = string.Empty;

    public ICollectionView ItemsView { get; }

    public RecentlyDeletedViewModel()
    {
        _apiService = App.ApiService;
        _signalRService = App.SignalRService;

        ItemsView = CollectionViewSource.GetDefaultView(_allItems);
        ItemsView.Filter = FilterItems;

        _signalRService.DataChanged += OnDataChanged;

        _ = LoadAsync();
    }

    private bool FilterItems(object obj)
    {
        if (obj is not RecentlyDeletedItem item) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var term = SearchText.Trim().ToLower();
        return item.Name.ToLower().Contains(term) ||
               item.Type.ToLower().Contains(term) ||
               item.DeletedBy.ToLower().Contains(term);
    }

    partial void OnSearchTextChanged(string value)
    {
        ItemsView.Refresh();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _apiService.GetAsync<List<RecentlyDeletedItem>>("api/recentlydeleted");
            _allItems.Clear();
            if (items != null)
            {
                _allItems.AddRange(items);
            }
            ItemsView.Refresh();
        }
        catch (Exception ex)
        {
            ClientLogger.Log("Failed to load recently deleted items", ex);
            ToastHelper.ShowToast("Failed to load recently deleted items.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Restore(RecentlyDeletedItem? item)
    {
        if (item is null) return;

        IsLoading = true;
        using (_signalRService.GetBusyScope())
        {
            try
            {
                await _apiService.PostAsync<object, object?>($"api/recentlydeleted/restore/{item.Type}/{item.Id}", new { });
                ToastHelper.ShowToast($"{item.Type} successfully restored.");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ClientLogger.Log($"Failed to restore {item.Type} ID: {item.Id}", ex);
                DialogHelper.ShowError($"Failed to restore record: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DeletePermanent(RecentlyDeletedItem? item)
    {
        if (item is null) return;

        var confirmMsg = $"Are you sure you want to permanently delete this {item.Type.ToLower()}?\n\nThis action CANNOT be undone and the record will be lost forever.";
        if (!DialogHelper.Confirm(confirmMsg, "Confirm Permanent Deletion")) return;

        IsLoading = true;
        using (_signalRService.GetBusyScope())
        {
            try
            {
                await _apiService.DeleteAsync($"api/recentlydeleted/permanent/{item.Type}/{item.Id}");
                ToastHelper.ShowToast($"{item.Type} permanently deleted.");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ClientLogger.Log($"Failed to permanently delete {item.Type} ID: {item.Id}", ex);
                DialogHelper.ShowError($"Failed to permanently delete record: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void OnDataChanged(Shared.Models.DataEventModel dataEvent)
    {
        if (dataEvent.EntityType == "Customer" || dataEvent.EntityType == "Product" || dataEvent.EntityType == "ServiceEntry")
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _ = LoadAsync();
            });
        }
    }

    public void Dispose()
    {
        _signalRService.DataChanged -= OnDataChanged;
    }
}

public class RecentlyDeletedItem
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeletedBy { get; set; } = string.Empty;
    public DateTime DeletedDate { get; set; }
    public int DaysRemaining { get; set; }
}
