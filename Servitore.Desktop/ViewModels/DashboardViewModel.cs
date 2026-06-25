using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoNotifications))]
    private DashboardSummary? summary;

    [ObservableProperty]
    private bool isLoading;

    public bool HasNoNotifications =>
        Summary == null || Summary.RecentNotifications.Count == 0;

    public DashboardViewModel(ApiService apiService) => _apiService = apiService;

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
                    Summary = await _apiService.GetAsync<DashboardSummary>("api/dashboard/summary");
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load dashboard summary failed", ex);
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
            Summary = new DashboardSummary();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCustomerAsync()
    {
        var dialog = new Views.Dialogs.CustomerEditDialog(null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                await _apiService.PostAsync<object, object>("api/customers", dialog.Customer);
                await LoadAsync();
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
                await _apiService.PostAsync<object, object>("api/assets", dialog.Asset);
                await LoadAsync();
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
    private async Task CreateTicketAsync()
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
    private async Task CreateAmcAsync()
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
                await _apiService.PostAsync<object, object>("api/amc", requestBody);
                await LoadAsync();
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
}
