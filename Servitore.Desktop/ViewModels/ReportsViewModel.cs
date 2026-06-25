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

    // ── Assets ─────────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportAssetsExcelAsync()
        => await DownloadFileAsync("api/reports/assets/excel", "Assets.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportAssetsPdfAsync()
        => await DownloadFileAsync("api/reports/assets/pdf", "Assets.pdf",
            "PDF Files|*.pdf");

    // ── Service Tickets ────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportTicketsExcelAsync()
        => await DownloadFileAsync("api/reports/tickets/excel", "ServiceTickets.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportTicketsPdfAsync()
        => await DownloadFileAsync("api/reports/tickets/pdf", "ServiceTickets.pdf",
            "PDF Files|*.pdf");

    // ── Warranty ───────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportWarrantyExcelAsync()
        => await DownloadFileAsync("api/reports/warranty/excel", "Warranty.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportWarrantyPdfAsync()
        => await DownloadFileAsync("api/reports/warranty/pdf", "Warranty.pdf",
            "PDF Files|*.pdf");

    // ── AMC ────────────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportAmcExcelAsync()
        => await DownloadFileAsync("api/reports/amc/excel", "AMCContracts.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportAmcPdfAsync()
        => await DownloadFileAsync("api/reports/amc/pdf", "AMCContracts.pdf",
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

        try
        {
            var bytes = await _apiService.GetByteArrayAsync(endpoint);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            Helpers.DialogHelper.ShowInfo("Report exported successfully.");
        }
        catch (Exception ex)
        {
            Helpers.DialogHelper.ShowError($"Failed to download report: {ex.Message}");
        }
    }
}
