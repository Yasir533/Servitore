using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class ProductProfileViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly int _productId;

    [ObservableProperty]
    private ProductDetailsDto? profile;

    [ObservableProperty]
    private bool isLoading;

    private System.Windows.Media.ImageSource? _barcodeImage;
    public System.Windows.Media.ImageSource? BarcodeImage
    {
        get => _barcodeImage;
        set => SetProperty(ref _barcodeImage, value);
    }

    public ProductProfileViewModel(ApiService apiService, int productId)
    {
        _apiService = apiService;
        _productId = productId;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Profile = await _apiService.GetAsync<ProductDetailsDto>($"api/assets/{_productId}/profile");
            if (Profile != null)
            {
                await LoadBarcodeImageAsync();
            }
        }
        catch (Exception ex)
        {
            ClientLogger.Log("Failed to load product profile data", ex);
            Helpers.ToastHelper.ShowToast("Failed to load product profile.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadBarcodeImageAsync()
    {
        if (Profile is null) return;
        try
        {
            // Use the QR code generation endpoint
            var barcodeBytes = await _apiService.GetByteArrayAsync($"api/barcode/qr/{Profile.ProductCode}");
            if (barcodeBytes != null && barcodeBytes.Length > 0)
            {
                var image = new BitmapImage();
                using (var ms = new MemoryStream(barcodeBytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                image.Freeze();
                BarcodeImage = image;
            }
        }
        catch
        {
            // Ignore barcode load errors silently
        }
    }

    [RelayCommand]
    private void Back()
    {
        NavigationHelper.NavigateTo(new Views.ProductView());
    }

    // ── Documents Management ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task UploadDocumentAsync()
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "All Files|*.*"
        };
        if (ofd.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var doc = await _apiService.UploadFileAsync<ProductDocumentDto>($"api/assets/{_productId}/documents", ofd.FileName);
                if (doc != null)
                {
                    Profile?.Documents.Add(doc);
                    OnPropertyChanged(nameof(Profile));
                }
            }
            catch (Exception)
            {
                DialogHelper.ShowError("Unable to save changes. Please try again later.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DownloadDocumentAsync(ProductDocumentDto? doc)
    {
        if (doc is null) return;
        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            FileName = doc.FileName,
            Filter = "All Files|*.*"
        };
        if (sfd.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var bytes = await _apiService.GetByteArrayAsync($"api/assets/documents/{doc.Id}");
                await File.WriteAllBytesAsync(sfd.FileName, bytes);
                DialogHelper.ShowInfo("Document downloaded successfully.");
            }
            catch (Exception)
            {
                DialogHelper.ShowError("Unable to download document. Please try again later.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(ProductDocumentDto? doc)
    {
        if (doc is null) return;
        if (!DialogHelper.Confirm($"Are you sure you want to delete {doc.FileName}?", "Confirm Delete")) return;

        IsLoading = true;
        using (App.SignalRService.GetBusyScope())
        {
            try
            {
                await _apiService.DeleteAsync($"api/assets/documents/{doc.Id}");
                Profile?.Documents.Remove(doc);
                OnPropertyChanged(nameof(Profile));
            }
            catch (Exception)
            {
                DialogHelper.ShowError("Unable to save changes. Please try again later.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
