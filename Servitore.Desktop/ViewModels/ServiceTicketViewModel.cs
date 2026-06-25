using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class ServiceTicketViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly SignalRService _signalRService;
    private readonly ObservableCollection<TicketRow> _allTickets = new();

    public ICollectionView TicketsView { get; }

    public List<string> StatusFilters { get; } =
        new() { "All", "Open", "In Progress", "On Hold", "Resolved", "Closed" };

    [ObservableProperty]
    private bool isLoading;

    private string _selectedStatusFilter = "All";
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set { SetProperty(ref _selectedStatusFilter, value); TicketsView.Refresh(); }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); TicketsView.Refresh(); }
    }

    public ServiceTicketViewModel(ApiService apiService, SignalRService signalRService)
    {
        _apiService = apiService;
        _signalRService = signalRService;
        TicketsView = CollectionViewSource.GetDefaultView(_allTickets);
        TicketsView.Filter = FilterTicket;
        _signalRService.NotificationReceived += async _ => await LoadAsync();
    }

    private bool FilterTicket(object obj)
    {
        if (obj is not TicketRow t) return false;
        if (SelectedStatusFilter != "All" && t.Status != SelectedStatusFilter) return false;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.ToLower();
            if (!t.TicketNumber.ToLower().Contains(q) &&
                !(t.CustomerName?.ToLower().Contains(q) ?? false))
                return false;
        }
        return true;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _apiService.GetAsync<List<TicketRow>>("api/servicetickets");
            _allTickets.Clear();
            if (results is not null)
                foreach (var t in results) _allTickets.Add(t);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewTicket()
    {
        var dialog = new Views.Dialogs.TicketEditDialog(_apiService, null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var dto = new
                {
                    CustomerId = dialog.Ticket.CustomerId,
                    AssetId = dialog.Ticket.AssetId,
                    ProblemDescription = dialog.Ticket.ProblemDescription,
                    Status = Enum.Parse<TicketStatus>(dialog.Ticket.Status),
                    Priority = Enum.Parse<TicketPriority>(dialog.Ticket.Priority),
                    AssignedToUserId = dialog.Ticket.AssignedToUserId,
                    ResolutionNotes = dialog.Ticket.ResolutionNotes
                };
                await _apiService.PostAsync<object, object>("api/servicetickets", dto);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Helpers.DialogHelper.ShowError($"Failed to save ticket: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task ViewTicket(TicketRow? row)
    {
        if (row is null) return;

        IsLoading = true;
        try
        {
            var ticketDetails = await _apiService.GetAsync<TicketDetailsDto>($"api/servicetickets/{row.TicketId}");
            if (ticketDetails == null) return;

            var dialog = new Views.Dialogs.TicketEditDialog(_apiService, ticketDetails)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                var dto = new
                {
                    TicketId = dialog.Ticket.TicketId,
                    CustomerId = dialog.Ticket.CustomerId,
                    AssetId = dialog.Ticket.AssetId,
                    ProblemDescription = dialog.Ticket.ProblemDescription,
                    Status = Enum.Parse<TicketStatus>(dialog.Ticket.Status),
                    Priority = Enum.Parse<TicketPriority>(dialog.Ticket.Priority),
                    AssignedToUserId = dialog.Ticket.AssignedToUserId,
                    ResolutionNotes = dialog.Ticket.ResolutionNotes
                };
                await _apiService.PutAsync($"api/servicetickets/{row.TicketId}", dto);
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            Helpers.DialogHelper.ShowError($"Failed to update ticket: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CloseTicket(TicketRow? row)
    {
        if (row is null) return;
        await _apiService.PutAsync($"api/servicetickets/{row.TicketId}/close", new { });
        await LoadAsync();
    }

    public class TicketRow
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? AssetName { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
