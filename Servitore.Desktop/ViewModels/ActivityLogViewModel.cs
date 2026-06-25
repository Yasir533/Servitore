using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class ActivityLogViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<ActivityLogDto> _all = new();

    public ICollectionView LogsView { get; }

    [ObservableProperty] private bool isLoading;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set 
        { 
            SetProperty(ref _searchText, value); 
            LogsView.Refresh(); 
        }
    }

    private string _selectedModule = "All";
    public string SelectedModule
    {
        get => _selectedModule;
        set 
        { 
            SetProperty(ref _selectedModule, value); 
            LogsView.Refresh(); 
        }
    }

    public ObservableCollection<string> Modules { get; } = new()
    {
        "All", "Auth", "Customers", "Assets", "Service Tickets", "AMC", "Users", "Settings"
    };

    public ActivityLogViewModel(ApiService apiService)
    {
        _apiService = apiService;
        LogsView = CollectionViewSource.GetDefaultView(_all);
        LogsView.Filter = Filter;
    }

    private bool Filter(object obj)
    {
        if (obj is not ActivityLogDto log) return false;

        // Apply module filter
        if (SelectedModule != "All")
        {
            if (!string.Equals(log.Module, SelectedModule, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Apply search text filter
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var q = SearchText.ToLower();
        return log.UserName.ToLower().Contains(q) || 
               log.Module.ToLower().Contains(q) || 
               log.Action.ToLower().Contains(q) ||
               log.LogId.ToString().Contains(q);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var results = await _apiService.GetAsync<List<ActivityLogDto>>("api/activitylogs");
                    _all.Clear();
                    if (results is not null)
                    {
                        foreach (var log in results)
                        {
                            _all.Add(log);
                        }
                    }
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load activity log data failed", ex);
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
        }
        finally 
        { 
            IsLoading = false; 
        }
    }
}
