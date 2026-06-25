using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Services;
using Servitore.Desktop.ViewModels;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Views.Dialogs;

public partial class TicketEditDialog : Window
{
    private readonly ApiService _apiService;
    public TicketDetailsDto Ticket { get; }
    private bool _isLoaded = false;

    public TicketEditDialog(ApiService apiService, TicketDetailsDto? ticket = null)
    {
        InitializeComponent();
        _apiService = apiService;

        if (ticket != null)
        {
            Ticket = ticket;
            TitleText.Text = $"Edit Ticket: {ticket.TicketNumber}";
            ProblemBox.Text = ticket.ProblemDescription;
            ResolutionBox.Text = ticket.ResolutionNotes;
            
            // Set SLA view
            if (ticket.SlaDueDate.HasValue)
            {
                SlaPanel.Visibility = Visibility.Visible;
                var localDue = ticket.SlaDueDate.Value.ToLocalTime();
                SlaDueText.Text = localDue.ToString("dd MMM yyyy HH:mm");
                if (ticket.SlaBreached)
                {
                    SlaStatusText.Text = "SLA BREACHED";
                    SlaStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (DateTime.UtcNow > ticket.SlaDueDate.Value)
                {
                    SlaStatusText.Text = "SLA BREACHED";
                    SlaStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    var remain = ticket.SlaDueDate.Value - DateTime.UtcNow;
                    SlaStatusText.Text = $"On Track ({remain.Hours}h {remain.Minutes}m remaining)";
                    SlaStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
        }
        else
        {
            Ticket = new TicketDetailsDto();
            TitleText.Text = "Add New Ticket";
            StatusCombo.IsEnabled = false; // New tickets start as Open
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Load customers lookup
            var customers = await _apiService.GetAsync<List<CustomerLookupItem>>("api/customers");
            CustomerCombo.ItemsSource = customers;

            // 2. Load engineers (RoleId = 3)
            var users = await _apiService.GetAsync<List<UserLookupItem>>("api/users");
            var engineers = users?.Where(u => u.RoleName == "Engineer" || u.RoleId == 3).ToList() ?? new List<UserLookupItem>();
            EngineerCombo.ItemsSource = engineers;

            // 3. Set Combos selection
            if (Ticket.TicketId > 0)
            {
                CustomerCombo.SelectedValue = Ticket.CustomerId;
                await LoadAssetsForCustomerAsync(Ticket.CustomerId);
                AssetCombo.SelectedValue = Ticket.AssetId;
                EngineerCombo.SelectedValue = Ticket.AssignedToUserId;

                // Priority
                foreach (ComboBoxItem item in PriorityCombo.Items)
                {
                    if (item.Tag?.ToString() == Ticket.Priority)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }

                // Status
                foreach (ComboBoxItem item in StatusCombo.Items)
                {
                    if (item.Tag?.ToString() == Ticket.Status)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load lookup data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CustomerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded && Ticket.TicketId > 0) return;

        if (CustomerCombo.SelectedValue is int customerId)
        {
            AssetCombo.ItemsSource = null;
            await LoadAssetsForCustomerAsync(customerId);
        }
    }

    private async System.Threading.Tasks.Task LoadAssetsForCustomerAsync(int customerId)
    {
        try
        {
            var assets = await _apiService.GetAsync<List<AssetLookupItem>>($"api/assets/by-customer/{customerId}");
            AssetCombo.ItemsSource = assets;
            if (assets != null && assets.Count > 0)
            {
                AssetCombo.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load assets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var problem = ProblemBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(problem))
        {
            MessageBox.Show("Problem Description is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CustomerCombo.SelectedValue is not int customerId)
        {
            MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (AssetCombo.SelectedValue is not int assetId)
        {
            MessageBox.Show("Please select an asset product.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Ticket.CustomerId = customerId;
        Ticket.AssetId = assetId;
        Ticket.ProblemDescription = problem;

        var selectedCust = CustomerCombo.SelectedItem as CustomerLookupItem;
        Ticket.CustomerName = selectedCust?.CustomerName ?? string.Empty;

        var selectedAsset = AssetCombo.SelectedItem as AssetLookupItem;
        Ticket.AssetName = selectedAsset?.ProductName ?? string.Empty;

        var priorityItem = PriorityCombo.SelectedItem as ComboBoxItem;
        Ticket.Priority = priorityItem?.Tag?.ToString() ?? "Medium";

        var statusItem = StatusCombo.SelectedItem as ComboBoxItem;
        Ticket.Status = statusItem?.Tag?.ToString() ?? "Open";

        Ticket.AssignedToUserId = EngineerCombo.SelectedValue as int?;
        var selectedEng = EngineerCombo.SelectedItem as UserLookupItem;
        Ticket.AssignedToUserName = selectedEng?.FullName;

        Ticket.ResolutionNotes = ResolutionBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public class CustomerLookupItem
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class AssetLookupItem
    {
        public int AssetId { get; set; }
        public string ProductName { get; set; } = string.Empty;
    }

    public class UserLookupItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}
