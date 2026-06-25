using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class AssetViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly BarcodeService _barcodeService;
    private readonly ObservableCollection<AssetRow> _allAssets = new();

    public ICollectionView AssetsView { get; }

    [ObservableProperty]
    private bool isLoading;

    private string _scannedCode = string.Empty;
    public string ScannedCode
    {
        get => _scannedCode;
        set => SetProperty(ref _scannedCode, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            AssetsView.Refresh();
        }
    }

    public AssetViewModel(ApiService apiService, BarcodeService barcodeService)
    {
        _apiService = apiService;
        _barcodeService = barcodeService;
        AssetsView = CollectionViewSource.GetDefaultView(_allAssets);
        AssetsView.Filter = FilterAsset;
    }

    private bool FilterAsset(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not AssetRow a) return false;
        var q = SearchText.ToLower();
        return a.ProductName.ToLower().Contains(q)
            || a.AssetCode.ToLower().Contains(q)
            || (a.SerialNumber?.ToLower().Contains(q) ?? false)
            || (a.CustomerName?.ToLower().Contains(q) ?? false);
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
                    var results = await _apiService.GetAsync<List<AssetRow>>("api/assets");
                    _allAssets.Clear();
                    if (results is not null)
                        foreach (var a in results) _allAssets.Add(a);
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load asset data failed", ex);
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
    private async Task LookupByBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(ScannedCode)) return;
        try
        {
            var asset = await _apiService.GetAsync<AssetRow>($"api/assets/by-barcode/{ScannedCode}");
            if (asset is not null)
            {
                _allAssets.Clear();
                _allAssets.Add(asset);
            }
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to find asset by barcode.");
        }
    }

    [RelayCommand]
    private async Task AddAssetAsync()
    {
        var dialog = new Views.Dialogs.AssetEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var response = await _apiService.PostAsync<AssetRow, AssetRow>("api/assets", dialog.Asset);
                if (response is not null)
                {
                    _allAssets.Add(response);
                    AssetsView.Refresh();
                }
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
    private async Task EditAssetAsync(AssetRow? row)
    {
        if (row is null) return;

        var clone = new AssetRow
        {
            AssetId = row.AssetId,
            AssetCode = row.AssetCode,
            ProductName = row.ProductName,
            SerialNumber = row.SerialNumber,
            CustomerName = row.CustomerName,
            WarrantyStatus = row.WarrantyStatus,
            CustomerId = row.CustomerId,
            Status = row.Status,
            VendorName = row.VendorName,
            PurchaseDate = row.PurchaseDate
        };

        var dialog = new Views.Dialogs.AssetEditDialog(_apiService, clone)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                await _apiService.PutAsync($"api/assets/{row.AssetId}", dialog.Asset);
                row.ProductName = dialog.Asset.ProductName;
                row.AssetCode = dialog.Asset.AssetCode;
                row.SerialNumber = dialog.Asset.SerialNumber;
                row.CustomerId = dialog.Asset.CustomerId;
                row.CustomerName = dialog.Asset.CustomerName;
                row.Status = dialog.Asset.Status;
                row.VendorName = dialog.Asset.VendorName;
                row.PurchaseDate = dialog.Asset.PurchaseDate;
                AssetsView.Refresh();
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
    private async Task DeleteAsset(AssetRow? row)
    {
        if (row is null) return;
        if (!Helpers.DialogHelper.Confirm($"Are you sure you want to delete asset {row.AssetCode}?", "Confirm Delete")) return;

        IsLoading = true;
        try
        {
            await _apiService.DeleteAsync($"api/assets/{row.AssetId}");
            _allAssets.Remove(row);
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to delete asset. Please try again later.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewProfile(AssetRow? row)
    {
        if (row is null) return;
        Helpers.NavigationHelper.NavigateTo(new Views.AssetProfileView(row.AssetId));
    }

    public class AssetRow
    {
        public int AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? WarrantyStatus { get; set; }
        public int CustomerId { get; set; }
        public string Status { get; set; } = "Active";
        public string? VendorName { get; set; }
        public DateTime? PurchaseDate { get; set; }
    }
}
