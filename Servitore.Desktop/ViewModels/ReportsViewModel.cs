using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly ApiService _apiService;

    public ReportsViewModel(ApiService apiService) => _apiService = apiService;

    // ── Customers ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportCustomersExcelAsync()
        => await DownloadFileAsync("api/reports/customers/excel", "Customers.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportCustomersPdfAsync()
        => await DownloadFileAsync("api/reports/customers/pdf", "Customers.pdf",
            "PDF Files|*.pdf");

    // ── Products ───────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportAssetsExcelAsync()
        => await DownloadFileAsync("api/reports/assets/excel", "Products.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportAssetsPdfAsync()
        => await DownloadFileAsync("api/reports/assets/pdf", "Products.pdf",
            "PDF Files|*.pdf");

    // ── Service Entries ────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportTicketsExcelAsync()
        => await DownloadFileAsync("api/reports/tickets/excel", "ServiceEntries.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportTicketsPdfAsync()
        => await DownloadFileAsync("api/reports/tickets/pdf", "ServiceEntries.pdf",
            "PDF Files|*.pdf");

    // ── Shared download helper ─────────────────────────────────────────────────
    private async Task DownloadFileAsync(string endpoint, string defaultFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter   = filter
        };

        if (dialog.ShowDialog() != true) return;

        using (App.SignalRService.GetBusyScope())
        {
            try
            {
                var bytes = await _apiService.GetByteArrayAsync(endpoint);
                await File.WriteAllBytesAsync(dialog.FileName, bytes);
                Helpers.DialogHelper.ShowInfo("Report exported successfully.");
            }
            catch (Exception)
            {
                Helpers.DialogHelper.ShowError("Unable to download report. Please try again.");
            }
        }
    }
}
