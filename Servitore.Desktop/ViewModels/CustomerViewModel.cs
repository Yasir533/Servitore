using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class CustomerViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<CustomerRow> _allCustomers = new();

    public ICollectionView CustomersView { get; }

    [ObservableProperty]
    private CustomerRow? selectedCustomer;

    [ObservableProperty]
    private bool isLoading;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            CustomersView.Refresh();
        }
    }

    public CustomerViewModel(ApiService apiService)
    {
        _apiService = apiService;
        CustomersView = CollectionViewSource.GetDefaultView(_allCustomers);
        CustomersView.Filter = FilterCustomer;
    }

    private bool FilterCustomer(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not CustomerRow c) return false;
        var q = SearchText.ToLower();
        return c.CustomerName.ToLower().Contains(q)
            || (c.Mobile?.ToLower().Contains(q) ?? false)
            || (c.Email?.ToLower().Contains(q) ?? false)
            || (c.ContactPerson?.ToLower().Contains(q) ?? false);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var results = await _apiService.GetAsync<List<CustomerRow>>("api/customers");
                    _allCustomers.Clear();
                    if (results is not null)
                        foreach (var c in results) _allCustomers.Add(c);
                    return; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load customer data failed", ex);
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
                var response = await _apiService.PostAsync<CustomerRow, CustomerRow>("api/customers", dialog.Customer);
                if (response is not null)
                {
                    _allCustomers.Add(response);
                    CustomersView.Refresh();
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
    private async Task EditCustomerAsync(CustomerRow? row)
    {
        if (row is null) return;
        
        var clone = new CustomerRow
        {
            CustomerId = row.CustomerId,
            CustomerName = row.CustomerName,
            ContactPerson = row.ContactPerson,
            Mobile = row.Mobile,
            Email = row.Email,
            Address = row.Address
        };

        var dialog = new Views.Dialogs.CustomerEditDialog(clone)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                await _apiService.PutAsync($"api/customers/{row.CustomerId}", dialog.Customer);
                row.CustomerName = dialog.Customer.CustomerName;
                row.ContactPerson = dialog.Customer.ContactPerson;
                row.Mobile = dialog.Customer.Mobile;
                row.Email = dialog.Customer.Email;
                row.Address = dialog.Customer.Address;
                CustomersView.Refresh();
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
    private async Task DeleteCustomer(CustomerRow? row)
    {
        if (row is null) return;
        if (!Helpers.DialogHelper.Confirm($"Are you sure you want to delete {row.CustomerName}?", "Confirm Delete")) return;
        
        IsLoading = true;
        try
        {
            await _apiService.DeleteAsync($"api/customers/{row.CustomerId}");
            _allCustomers.Remove(row);
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to delete customer. Please try again later.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewProfile(CustomerRow? row)
    {
        if (row is null) return;
        NavigationHelper.NavigateTo(new Views.CustomerProfileView(row.CustomerId));
    }

    public class CustomerRow
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}
