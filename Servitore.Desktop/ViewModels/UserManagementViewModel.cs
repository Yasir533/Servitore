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

public partial class UserManagementViewModel : ViewModelBase
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<UserRow> _all = new();

    public ICollectionView UsersView { get; }

    [ObservableProperty] private bool isLoading;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); UsersView.Refresh(); }
    }

    public UserManagementViewModel(ApiService apiService)
    {
        _apiService = apiService;
        UsersView = CollectionViewSource.GetDefaultView(_all);
        UsersView.Filter = Filter;
    }

    private bool Filter(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not UserRow u) return false;
        var q = SearchText.ToLower();
        return u.FullName.ToLower().Contains(q) || u.Username.ToLower().Contains(q);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _apiService.GetAsync<List<UserRow>>("api/users");
            _all.Clear();
            if (results is not null)
            {
                foreach (var u in results) _all.Add(u);
            }
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log("Failed to load user data", ex);
            Helpers.ToastHelper.ShowToast("Failed to load users.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddUser()
    {
        var dialog = new Views.Dialogs.UserEditDialog(null)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var roleId = dialog.User.RoleName switch
                {
                    "Admin" => 1,
                    "Manager" => 2,
                    "Engineer" => 3,
                    _ => 4 // Operator
                };

                var dto = new
                {
                    Username = dialog.User.Username,
                    FullName = dialog.User.FullName,
                    Email = dialog.User.Email,
                    PhoneNumber = dialog.User.PhoneNumber,
                    RoleId = roleId,
                    IsActive = dialog.User.IsActive,
                    Password = dialog.Password
                };

                await _apiService.PostAsync<object, object>("api/users", dto);
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
    private async Task EditUser(UserRow? row)
    {
        if (row is null) return;

        var clone = new UserRow
        {
            Id = row.Id,
            Username = row.Username,
            FullName = row.FullName,
            Email = row.Email,
            PhoneNumber = row.PhoneNumber,
            RoleName = row.RoleName,
            IsActive = row.IsActive
        };

        var dialog = new Views.Dialogs.UserEditDialog(clone)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var roleId = dialog.User.RoleName switch
                {
                    "Admin" => 1,
                    "Manager" => 2,
                    "Engineer" => 3,
                    _ => 4 // Operator
                };

                var dto = new
                {
                    Id = dialog.User.Id,
                    Username = dialog.User.Username,
                    FullName = dialog.User.FullName,
                    Email = dialog.User.Email,
                    PhoneNumber = dialog.User.PhoneNumber,
                    RoleId = roleId,
                    IsActive = dialog.User.IsActive,
                    Password = dialog.Password
                };

                await _apiService.PutAsync($"api/users/{row.Id}", dto);
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
    private async Task ToggleActive(UserRow? row)
    {
        if (row is null) return;
        try
        {
            await _apiService.PutAsync($"api/users/{row.Id}/toggle-active", new { });
            row.IsActive = !row.IsActive;
            UsersView.Refresh();
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to toggle user active status. Please try again later.");
        }
    }

    public class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; }
    }
}
