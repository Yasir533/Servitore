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

public partial class AssetProfileViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly int _assetId;

    [ObservableProperty]
    private AssetDetailsDto? profile;

    [ObservableProperty]
    private bool isLoading;

    private System.Windows.Media.ImageSource? _barcodeImage;
    public System.Windows.Media.ImageSource? BarcodeImage
    {
        get => _barcodeImage;
        set => SetProperty(ref _barcodeImage, value);
    }

    public AssetProfileViewModel(ApiService apiService, int assetId)
    {
        _apiService = apiService;
        _assetId = assetId;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            int maxRetries = 15;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Profile = await _apiService.GetAsync<AssetDetailsDto>($"api/assets/{_assetId}/profile");
                    if (Profile != null)
                    {
                        await LoadBarcodeImageAsync();
                    }
                    return; // Success!
                }
                catch (Exception ex)
                {
                    ClientLogger.Log($"Attempt {i + 1} to load asset profile data failed", ex);
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

    private async Task LoadBarcodeImageAsync()
    {
        if (Profile is null) return;
        try
        {
            // Use the QR code generation endpoint
            var barcodeBytes = await _apiService.GetByteArrayAsync($"api/barcode/qr/{Profile.AssetCode}");
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
        NavigationHelper.NavigateTo(new Views.AssetView());
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
                var doc = await _apiService.UploadFileAsync<AssetDocumentDto>($"api/assets/{_assetId}/documents", ofd.FileName);
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
    private async Task DownloadDocumentAsync(AssetDocumentDto? doc)
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
    private async Task DeleteDocumentAsync(AssetDocumentDto? doc)
    {
        if (doc is null) return;
        if (!DialogHelper.Confirm($"Are you sure you want to delete {doc.FileName}?", "Confirm Delete")) return;

        IsLoading = true;
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

    // ── AMC Visits Management ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScheduleVisitAsync()
    {
        if (Profile?.AMCContract is null)
        {
            DialogHelper.ShowError("There is no active AMC contract for this asset.");
            return;
        }

        var dialog = new Views.Dialogs.AMCVisitDialog(_apiService, Profile.AMCContract.AMCContractId, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var visit = new
                {
                    ScheduledDate = dialog.Visit.ScheduledDate,
                    Status = dialog.Visit.Status,
                    Remarks = dialog.Visit.Remarks,
                    EngineerId = dialog.Visit.EngineerId
                };
                var response = await _apiService.PostAsync<object, AMCVisitDto>($"api/amc/{Profile.AMCContract.AMCContractId}/visits", visit);
                if (response is not null)
                {
                    Profile.AMCContract.Visits.Add(response);
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
    private async Task EditVisitAsync(AMCVisitDto? row)
    {
        if (row is null || Profile?.AMCContract is null) return;

        var dialog = new Views.Dialogs.AMCVisitDialog(_apiService, Profile.AMCContract.AMCContractId, row)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var visit = new
                {
                    ScheduledDate = dialog.Visit.ScheduledDate,
                    VisitDate = dialog.Visit.VisitDate,
                    Status = dialog.Visit.Status,
                    Remarks = dialog.Visit.Remarks,
                    EngineerId = dialog.Visit.EngineerId
                };
                await _apiService.PutAsync($"api/amc/visits/{row.Id}", visit);
                
                row.ScheduledDate = dialog.Visit.ScheduledDate;
                row.VisitDate = dialog.Visit.VisitDate;
                row.Status = dialog.Visit.Status;
                row.Remarks = dialog.Visit.Remarks;
                row.EngineerName = dialog.Visit.EngineerName;
                
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

    [RelayCommand]
    private async Task DeleteVisitAsync(AMCVisitDto? row)
    {
        if (row is null) return;
        if (!DialogHelper.Confirm("Are you sure you want to cancel/delete this scheduled visit?", "Confirm Delete")) return;

        IsLoading = true;
        try
        {
            await _apiService.DeleteAsync($"api/amc/visits/{row.Id}");
            Profile?.AMCContract?.Visits.Remove(row);
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
