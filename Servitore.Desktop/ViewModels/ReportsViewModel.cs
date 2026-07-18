using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private string _selectedStatus = "All";

    [ObservableProperty]
    private string _selectedPriority = "All";

    public string[] Statuses { get; } = { "All", "Pending", "InProgress", "Completed", "Delivered" };
    public string[] Priorities { get; } = { "All", "Low", "Normal", "High", "Urgent" };

    public ReportsViewModel(ApiService apiService) => _apiService = apiService;

    [RelayCommand]
    private void ResetFilters()
    {
        StartDate = null;
        EndDate = null;
        SelectedStatus = "All";
        SelectedPriority = "All";
    }

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
        => await DownloadFileAsync(BuildTicketsUrl("excel"), "ServiceEntries.xlsx",
            "Excel Files|*.xlsx");

    [RelayCommand]
    private async Task ExportTicketsPdfAsync()
        => await DownloadFileAsync(BuildTicketsUrl("pdf"), "ServiceEntries.pdf",
            "PDF Files|*.pdf");

    private string BuildTicketsUrl(string format)
    {
        var sb = new StringBuilder($"api/reports/tickets/{format}");
        var queryParams = new List<string>();

        if (StartDate.HasValue)
            queryParams.Add($"startDate={StartDate.Value:yyyy-MM-dd}");

        if (EndDate.HasValue)
            queryParams.Add($"endDate={EndDate.Value:yyyy-MM-dd}");

        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All")
        {
            if (Enum.TryParse<Servitore.Shared.Enums.ServiceEntryStatus>(SelectedStatus, out var statusEnum))
            {
                queryParams.Add($"status={(int)statusEnum}");
            }
        }

        if (!string.IsNullOrEmpty(SelectedPriority) && SelectedPriority != "All")
        {
            if (Enum.TryParse<Servitore.Shared.Enums.ServiceEntryPriority>(SelectedPriority, out var priorityEnum))
            {
                queryParams.Add($"priority={(int)priorityEnum}");
            }
        }

        if (queryParams.Count > 0)
        {
            sb.Append("?");
            sb.Append(string.Join("&", queryParams));
        }

        return sb.ToString();
    }

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
            catch (Exception ex)
            {
                Helpers.ClientLogger.Log($"Failed to download report from {endpoint}", ex);
                Helpers.DialogHelper.ShowError("Unable to download report. Please try again.");
            }
        }
    }
}
