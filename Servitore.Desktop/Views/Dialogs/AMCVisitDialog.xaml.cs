using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Services;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Views.Dialogs;

public partial class AMCVisitDialog : Window
{
    private readonly ApiService _apiService;
    private readonly int _contractId;
    public AMCVisitDto Visit { get; }

    public AMCVisitDialog(ApiService apiService, int contractId, AMCVisitDto? visit = null)
    {
        InitializeComponent();
        _apiService = apiService;
        _contractId = contractId;

        if (visit != null)
        {
            Visit = visit;
            TitleText.Text = "Update AMC Visit";
            ScheduledDatePicker.SelectedDate = visit.ScheduledDate;
            ActualVisitDatePicker.SelectedDate = visit.VisitDate;
            RemarksBox.Text = visit.Remarks;
        }
        else
        {
            Visit = new AMCVisitDto
            {
                AMCContractId = contractId,
                ScheduledDate = DateTime.Today,
                Status = "Scheduled"
            };
            TitleText.Text = "Schedule AMC Visit";
            ScheduledDatePicker.SelectedDate = Visit.ScheduledDate;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Load engineers lookup list
            var users = await _apiService.GetAsync<List<UserLookupItem>>("api/users");
            var engineers = users?.Where(u => u.RoleName == "Engineer" || u.RoleId == 3).ToList() ?? new List<UserLookupItem>();
            EngineerCombo.ItemsSource = engineers;

            if (Visit.Id > 0)
            {
                // Select assigned engineer
                var engineer = engineers.FirstOrDefault(eng => eng.FullName == Visit.EngineerName);
                if (engineer != null)
                {
                    EngineerCombo.SelectedValue = engineer.Id;
                }

                // Select visit status
                foreach (ComboBoxItem item in StatusCombo.Items)
                {
                    if (item.Tag?.ToString() == Visit.Status)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load lookup data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ScheduledDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Scheduled Date is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StatusCombo.SelectedItem is not ComboBoxItem statusItem)
        {
            MessageBox.Show("Please select a visit status.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var status = statusItem.Tag?.ToString() ?? "Scheduled";
        var visitDate = ActualVisitDatePicker.SelectedDate;

        if (status == "Completed" && !visitDate.HasValue)
        {
            MessageBox.Show("Actual Visit Date is required when status is Completed.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Visit.ScheduledDate = ScheduledDatePicker.SelectedDate.Value;
        Visit.VisitDate = visitDate;
        Visit.Status = status;
        Visit.Remarks = RemarksBox.Text.Trim();

        if (EngineerCombo.SelectedValue is int engineerId)
        {
            Visit.EngineerId = engineerId;
            var selectedEng = EngineerCombo.SelectedItem as UserLookupItem;
            Visit.EngineerName = selectedEng?.FullName;
        }
        else
        {
            Visit.EngineerId = null;
            Visit.EngineerName = null;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public class UserLookupItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}
