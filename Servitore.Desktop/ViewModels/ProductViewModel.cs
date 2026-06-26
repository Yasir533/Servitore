using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class ProductViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly BarcodeService _barcodeService;
    private readonly ObservableCollection<ProductRow> _allProducts = new();

    public ICollectionView ProductsView { get; }

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
            ProductsView.Refresh();
        }
    }

    public ProductViewModel(ApiService apiService, BarcodeService barcodeService)
    {
        _apiService = apiService;
        _barcodeService = barcodeService;
        ProductsView = CollectionViewSource.GetDefaultView(_allProducts);
        ProductsView.Filter = FilterProduct;
        App.SignalRService.DataChanged += OnDataChanged;
    }

    private async void OnDataChanged(Servitore.Shared.Models.DataEventModel dataEvent)
    {
        if (dataEvent.EntityType == "Asset")
        {
            await LoadAsync();
        }
    }

    private bool FilterProduct(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not ProductRow a) return false;
        var q = SearchText.ToLower();
        return a.ProductName.ToLower().Contains(q)
            || a.ProductCode.ToLower().Contains(q)
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
                    var results = await _apiService.GetAsync<List<ProductRow>>("api/assets");
                    _allProducts.Clear();
                    if (results is not null)
                        foreach (var a in results) _allProducts.Add(a);
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load product data failed", ex);
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
            var product = await _apiService.GetAsync<ProductRow>($"api/assets/by-barcode/{ScannedCode}");
            if (product is not null)
            {
                _allProducts.Clear();
                _allProducts.Add(product);
            }
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to find product by barcode.");
        }
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        var dialog = new Views.Dialogs.ProductEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var response = await _apiService.PostAsync<ProductRow, ProductRow>("api/assets", dialog.Product);
                if (response is not null)
                {
                    _allProducts.Add(response);
                    ProductsView.Refresh();

                    await App.SignalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
                    {
                        EntityType = "Asset",
                        Action = "Created",
                        RecordId = response.ProductId.ToString(),
                        DisplayName = response.ProductName,
                        Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
                    });
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
    private async Task EditProductAsync(ProductRow? row)
    {
        if (row is null) return;

        var clone = new ProductRow
        {
            ProductId = row.ProductId,
            ProductCode = row.ProductCode,
            ProductName = row.ProductName,
            SerialNumber = row.SerialNumber,
            CustomerName = row.CustomerName,
            WarrantyStatus = row.WarrantyStatus,
            CustomerId = row.CustomerId,
            Status = row.Status,
            VendorName = row.VendorName,
            PurchaseDate = row.PurchaseDate,
            ModifiedDate = row.ModifiedDate
        };

        var dialog = new Views.Dialogs.ProductEditDialog(_apiService, clone)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            row.ProductName = dialog.Product.ProductName;
            row.ProductCode = dialog.Product.ProductCode;
            row.SerialNumber = dialog.Product.SerialNumber;
            row.CustomerId = dialog.Product.CustomerId;
            row.CustomerName = dialog.Product.CustomerName;
            row.Status = dialog.Product.Status;
            row.VendorName = dialog.Product.VendorName;
            row.PurchaseDate = dialog.Product.PurchaseDate;
            row.ModifiedDate = dialog.Product.ModifiedDate;
            ProductsView.Refresh();

            await App.SignalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
            {
                EntityType = "Asset",
                Action = "Updated",
                RecordId = row.ProductId.ToString(),
                DisplayName = row.ProductName,
                Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
            });
        }
    }

    [RelayCommand]
    private async Task DeleteProduct(ProductRow? row)
    {
        if (row is null) return;
        if (!Helpers.DialogHelper.Confirm($"Are you sure you want to delete product {row.ProductCode}?", "Confirm Delete")) return;

        IsLoading = true;
        try
        {
            var id = row.ProductId;
            var code = row.ProductCode;
            await _apiService.DeleteAsync($"api/assets/{row.ProductId}");
            _allProducts.Remove(row);

            await App.SignalRService.BroadcastDataChangeAsync(new Servitore.Shared.Models.DataEventModel
            {
                EntityType = "Asset",
                Action = "Deleted",
                RecordId = id.ToString(),
                DisplayName = code,
                Username = App.AuthenticationService.CurrentUser?.FullName ?? "Unknown"
            });
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to delete product. Please try again later.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewProfile(ProductRow? row)
    {
        if (row is null) return;
        Helpers.NavigationHelper.NavigateTo(new Views.ProductProfileView(row.ProductId));
    }

    public class ProductRow
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? WarrantyStatus { get; set; }
        public int CustomerId { get; set; }
        public string Status { get; set; } = "Active";
        public string? VendorName { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
