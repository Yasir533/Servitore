using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class AMCViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<AMCRow> _all = new();

    public ICollectionView ContractsView { get; }

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private int activeCount;
    [ObservableProperty] private int expiringCount;
    [ObservableProperty] private decimal totalValue;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ContractsView.Refresh(); }
    }

    public AMCViewModel(ApiService apiService)
    {
        _apiService = apiService;
        ContractsView = CollectionViewSource.GetDefaultView(_all);
        ContractsView.Filter = Filter;
    }

    private bool Filter(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not AMCRow a) return false;
        var q = SearchText.ToLower();
        return (a.AssetName?.ToLower().Contains(q) ?? false)
            || (a.CustomerName?.ToLower().Contains(q) ?? false);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _apiService.GetAsync<List<AMCRow>>("api/amc");
            _all.Clear();
            if (results is not null)
            {
                foreach (var r in results) _all.Add(r);
                ActiveCount   = results.Count(r => r.Status == "Active");
                ExpiringCount = results.Count(r => r.Status == "Expiring Soon");
                TotalValue    = results.Where(r => r.Status == "Active").Sum(r => r.ContractValue);
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task AddContractAsync()
    {
        var dialog = new Views.Dialogs.AMCEditDialog(_apiService, null)
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
                    AssetId = dialog.Contract.AssetId,
                    StartDate = dialog.Contract.StartDate,
                    EndDate = dialog.Contract.EndDate,
                    ContractValue = dialog.Contract.ContractValue,
                    VisitsIncluded = dialog.Contract.VisitsIncluded,
                    Status = dialog.Contract.Status
                };
                var response = await _apiService.PostAsync<object, AMCRow>("api/amc", requestBody);
                if (response is not null)
                {
                    _all.Add(response);
                    ContractsView.Refresh();
                }
            }
            catch (Exception ex)
            {
                Helpers.DialogHelper.ShowError($"Failed to save AMC contract: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task EditContractAsync(AMCRow? row)
    {
        if (row is null) return;

        var clone = new AMCRow
        {
            AMCContractId = row.AMCContractId,
            AssetName = row.AssetName,
            CustomerName = row.CustomerName,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            ContractValue = row.ContractValue,
            VisitsIncluded = row.VisitsIncluded,
            DaysRemaining = row.DaysRemaining,
            Status = row.Status,
            CustomerId = row.CustomerId,
            AssetId = row.AssetId
        };

        var dialog = new Views.Dialogs.AMCEditDialog(_apiService, clone)
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
                    AMCContractId = dialog.Contract.AMCContractId,
                    AssetId = dialog.Contract.AssetId,
                    StartDate = dialog.Contract.StartDate,
                    EndDate = dialog.Contract.EndDate,
                    ContractValue = dialog.Contract.ContractValue,
                    VisitsIncluded = dialog.Contract.VisitsIncluded,
                    Status = dialog.Contract.Status
                };
                await _apiService.PutAsync($"api/amc/{row.AMCContractId}", requestBody);
                row.StartDate = dialog.Contract.StartDate;
                row.EndDate = dialog.Contract.EndDate;
                row.ContractValue = dialog.Contract.ContractValue;
                row.VisitsIncluded = dialog.Contract.VisitsIncluded;
                row.AssetName = dialog.Contract.AssetName;
                row.CustomerName = dialog.Contract.CustomerName;
                row.Status = dialog.Contract.Status;
                row.CustomerId = dialog.Contract.CustomerId;
                row.AssetId = dialog.Contract.AssetId;
                ContractsView.Refresh();
            }
            catch (Exception ex)
            {
                Helpers.DialogHelper.ShowError($"Failed to update AMC contract: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteContract(AMCRow? row)
    {
        if (row is null) return;
        if (!Helpers.DialogHelper.Confirm("Are you sure you want to delete this contract?", "Confirm Delete")) return;

        IsLoading = true;
        try
        {
            await _apiService.DeleteAsync($"api/amc/{row.AMCContractId}");
            _all.Remove(row);
        }
        catch (Exception ex)
        {
            Helpers.DialogHelper.ShowError($"Failed to delete AMC contract: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    public class AMCRow
    {
        public int AMCContractId { get; set; }
        public string? AssetName { get; set; }
        public string? CustomerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal ContractValue { get; set; }
        public int VisitsIncluded { get; set; }
        public int DaysRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int AssetId { get; set; }
    }
}
