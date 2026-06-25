using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class WarrantyViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<WarrantyRow> _all = new();

    public ICollectionView WarrantiesView { get; }

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private int expiredCount;
    [ObservableProperty] private int expiringCount;
    [ObservableProperty] private int activeCount;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); WarrantiesView.Refresh(); }
    }

    public WarrantyViewModel(ApiService apiService)
    {
        _apiService = apiService;
        WarrantiesView = CollectionViewSource.GetDefaultView(_all);
        WarrantiesView.Filter = Filter;
        App.SignalRService.DataChanged += OnDataChanged;
    }

    private async void OnDataChanged(Servitore.Shared.Models.DataEventModel dataEvent)
    {
        if (dataEvent.EntityType == "Warranty")
        {
            await LoadAsync();
        }
    }

    private bool Filter(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not WarrantyRow w) return false;
        var q = SearchText.ToLower();
        return (w.AssetName?.ToLower().Contains(q) ?? false)
            || (w.CustomerName?.ToLower().Contains(q) ?? false);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            int maxRetries = 15;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var results = await _apiService.GetAsync<List<WarrantyRow>>("api/warranty");
                    _all.Clear();
                    if (results is not null)
                    {
                        foreach (var r in results) _all.Add(r);
                        ExpiredCount  = results.Count(r => r.WarrantyStatus == "Expired");
                        ExpiringCount = results.Count(r => r.WarrantyStatus == "Expiring Soon");
                        ActiveCount   = results.Count(r => r.WarrantyStatus == "Active");
                    }
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load warranty data failed", ex);
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

    [RelayCommand]
    private async Task AddWarrantyAsync()
    {
        var dialog = new Views.Dialogs.WarrantyEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var requestBody = new
                {
                    AssetId = dialog.Warranty.AssetId,
                    StartDate = dialog.Warranty.StartDate,
                    EndDate = dialog.Warranty.EndDate,
                    Terms = dialog.Warranty.Terms,
                    VendorName = dialog.Warranty.VendorName
                };
                await _apiService.PostAsync<object, object>("api/warranty", requestBody);
                await LoadAsync();

                await App.SignalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
                {
                    EntityType = "Warranty",
                    Action = "Created",
                    RecordId = "New",
                    DisplayName = $"Warranty - {dialog.Warranty.AssetName}",
                    Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
                });
            }
            catch (Exception)
            {
                Helpers.DialogHelper.ShowError("Unable to save changes. Please try again later.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task EditWarrantyAsync(WarrantyRow? row)
    {
        if (row is null) return;

        var clone = new WarrantyRow
        {
            WarrantyId = row.WarrantyId,
            CustomerId = row.CustomerId,
            AssetId = row.AssetId,
            AssetName = row.AssetName,
            CustomerName = row.CustomerName,
            SerialNumber = row.SerialNumber,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            Terms = row.Terms,
            VendorName = row.VendorName,
            DaysRemaining = row.DaysRemaining,
            WarrantyStatus = row.WarrantyStatus
        };

        var dialog = new Views.Dialogs.WarrantyEditDialog(_apiService, clone)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var requestBody = new
                {
                    WarrantyId = dialog.Warranty.WarrantyId,
                    AssetId = dialog.Warranty.AssetId,
                    StartDate = dialog.Warranty.StartDate,
                    EndDate = dialog.Warranty.EndDate,
                    Terms = dialog.Warranty.Terms,
                    VendorName = dialog.Warranty.VendorName
                };
                await _apiService.PutAsync($"api/warranty/{row.WarrantyId}", requestBody);
                await LoadAsync();

                await App.SignalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
                {
                    EntityType = "Warranty",
                    Action = "Updated",
                    RecordId = row.WarrantyId.ToString(),
                    DisplayName = $"Warranty - {row.AssetName}",
                    Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
                });
            }
            catch (Exception)
            {
                Helpers.DialogHelper.ShowError("Unable to save changes. Please try again later.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteWarrantyAsync(WarrantyRow? row)
    {
        if (row is null) return;
        if (!Helpers.DialogHelper.Confirm("Are you sure you want to delete this warranty record?", "Confirm Delete")) return;

        IsLoading = true;
        try
        {
            var id = row.WarrantyId;
            var assetName = row.AssetName;
            await _apiService.DeleteAsync($"api/warranty/{row.WarrantyId}");
            await LoadAsync();

            await App.SignalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
            {
                EntityType = "Warranty",
                Action = "Deleted",
                RecordId = id.ToString(),
                DisplayName = $"Warranty - {assetName}",
                Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
            });
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to delete warranty. Please try again later.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public class WarrantyRow
    {
        public int WarrantyId { get; set; }
        public int CustomerId { get; set; }
        public int AssetId { get; set; }
        public string? AssetName { get; set; }
        public string? CustomerName { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Terms { get; set; }
        public string? VendorName { get; set; }
        public int DaysRemaining { get; set; }
        public string WarrantyStatus { get; set; } = string.Empty;
    }
}
