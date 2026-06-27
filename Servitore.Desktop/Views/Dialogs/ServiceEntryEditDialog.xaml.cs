using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Servitore.Desktop.Helpers;
using Servitore.Desktop.Services;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Views.Dialogs;

public partial class ServiceEntryEditDialog : Window
{
    private readonly ApiService _apiService;
    public ServiceEntryDetailsDto ServiceEntry { get; private set; }
    private readonly ServiceEntryDetailsDto _initialServiceEntry;
    private bool _isLoaded = false;
    private bool _isDirty = false;
    private bool _isClosingFromSave = false;
    private string _recordKey = string.Empty;
    private bool _isReadOnly = false;
    private List<CustomerDetails> _allCustomers = new();
    private List<AssetLookupItem> _customerAssets = new();
    private System.Threading.CancellationTokenSource? _debounceCts = null;

    private readonly IDisposable _busyScopeValue;

    public ServiceEntryEditDialog(ApiService apiService, ServiceEntryDetailsDto? serviceEntry = null)
    {
        InitializeComponent();
        _busyScopeValue = App.SignalRService.GetBusyScope();
        _apiService = apiService;

        if (serviceEntry != null)
        {
            ServiceEntry = serviceEntry;
            TitleText.Text = $"Edit Service Entry: {serviceEntry.ServiceEntryNumber}";
            
            // Set SLA view
            if (serviceEntry.SlaDueDate.HasValue)
            {
                SlaPanel.Visibility = Visibility.Visible;
                var localDue = serviceEntry.SlaDueDate.Value.ToLocalTime();
                SlaDueText.Text = localDue.ToString("dd MMM yyyy HH:mm");
                if (serviceEntry.SlaBreached)
                {
                    SlaStatusText.Text = "SLA BREACHED";
                    SlaStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (DateTime.UtcNow > serviceEntry.SlaDueDate.Value)
                {
                    SlaStatusText.Text = "SLA BREACHED";
                    SlaStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    var remain = serviceEntry.SlaDueDate.Value - DateTime.UtcNow;
                    SlaStatusText.Text = $"On Track ({remain.Hours}h {remain.Minutes}m remaining)";
                    SlaStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
        }
        else
        {
            ServiceEntry = new ServiceEntryDetailsDto();
            TitleText.Text = "Add New Service Entry";
        }

        // Deep clone the initial state for Reset functionality
        _initialServiceEntry = new ServiceEntryDetailsDto
        {
            ServiceEntryId = ServiceEntry.ServiceEntryId,
            ServiceEntryNumber = ServiceEntry.ServiceEntryNumber,
            
            CustomerId = ServiceEntry.CustomerId,
            CustomerName = ServiceEntry.CustomerName,
            CustomerMobile = ServiceEntry.CustomerMobile,
            CustomerCompany = ServiceEntry.CustomerCompany,
            CustomerEmail = ServiceEntry.CustomerEmail,

            ProductId = ServiceEntry.ProductId,
            ProductName = ServiceEntry.ProductName,
            ProductBrand = ServiceEntry.ProductBrand,
            ProductModel = ServiceEntry.ProductModel,
            ProductSerialNumber = ServiceEntry.ProductSerialNumber,

            ProblemDescription = ServiceEntry.ProblemDescription,
            AccessoriesReceived = ServiceEntry.AccessoriesReceived,
            Remarks = ServiceEntry.Remarks,
            Solution = ServiceEntry.Solution,

            Priority = ServiceEntry.Priority,
            Status = ServiceEntry.Status,
            AssignedToUserId = ServiceEntry.AssignedToUserId,
            AssignedToUserName = ServiceEntry.AssignedToUserName,
            SlaDueDate = ServiceEntry.SlaDueDate,
            SlaBreached = ServiceEntry.SlaBreached,
            Attachments = ServiceEntry.Attachments.ToList()
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Load customers list for autocomplete search
            var customers = await _apiService.GetAsync<List<CustomerDetails>>("api/customers");
            if (customers != null)
            {
                _allCustomers = customers;
            }

            // 2. Load engineers using role-independent lookup
            var users = await _apiService.GetAsync<List<UserInfo>>("api/users/lookup");
            var engineers = users?.Where(u => u.Role == UserRole.Engineer).ToList() ?? new List<UserInfo>();
            EngineerCombo.ItemsSource = engineers;

            // 3. Populate form controls
            if (ServiceEntry.ServiceEntryId > 0)
            {
                MobileBox.Text = ServiceEntry.CustomerMobile;
                NameBox.Text = ServiceEntry.CustomerName;
                CompanyBox.Text = ServiceEntry.CustomerCompany;
                EmailBox.Text = ServiceEntry.CustomerEmail;

                ProductNameBox.Text = ServiceEntry.ProductName;
                BrandBox.Text = ServiceEntry.ProductBrand;
                ModelBox.Text = ServiceEntry.ProductModel;
                SerialNumberBox.Text = ServiceEntry.ProductSerialNumber;

                ProblemBox.Text = ServiceEntry.ProblemDescription;
                RemarksBox.Text = ServiceEntry.Remarks;
                SolutionBox.Text = ServiceEntry.Solution;

                // Load customer existing products
                await LoadAssetsForCustomerAsync(ServiceEntry.CustomerId);

                // Priority select
                foreach (ComboBoxItem item in PriorityCombo.Items)
                {
                    if (item.Tag?.ToString() == ServiceEntry.Priority.ToString())
                    {
                        item.IsSelected = true;
                        break;
                    }
                }

                // Status select
                foreach (ComboBoxItem item in StatusCombo.Items)
                {
                    if (item.Tag?.ToString() == ServiceEntry.Status.ToString())
                    {
                        item.IsSelected = true;
                        break;
                    }
                }

                EngineerCombo.SelectedValue = ServiceEntry.AssignedToUserId;

                // Load accessories
                LoadAccessories(ServiceEntry.AccessoriesReceived);

                // Bind attachments
                RefreshAttachmentsGrid();
                UploadButton.IsEnabled = true;

                // Bind history timeline
                if (ServiceEntry.History != null && ServiceEntry.History.Count > 0)
                {
                    HistoryGrid.ItemsSource = ServiceEntry.History.OrderByDescending(h => h.UpdatedDate).ToList();
                    HistoryCard.Visibility = Visibility.Visible;
                }
                else
                {
                    HistoryCard.Visibility = Visibility.Collapsed;
                }

                ProblemBox.Focus();
            }
            else
            {
                StatusCombo.IsEnabled = false; // Starts as Pending for new service entries
                UploadButton.IsEnabled = false;
                AttachmentsGrid.Visibility = Visibility.Collapsed;
                AttachmentsNote.Visibility = Visibility.Visible;
                
                // Focus Mobile number for fast entry
                MobileBox.Focus();
            }

            _isLoaded = true;
        }
        catch (Exception)
        {
            ShowValidationError("Unable to load lookup data. Please try again.");
        }

        if (ServiceEntry.ServiceEntryId > 0)
        {
            _recordKey = $"ServiceEntry-{ServiceEntry.ServiceEntryId}";
            var lockResult = await LockHelper.AcquireLockAsync(_recordKey);
            if (!lockResult.Success)
            {
                var lockOwner = lockResult.Lock?.Username ?? "another user";
                var lockTime = lockResult.Lock?.LockedAt.ToLocalTime().ToString("t") ?? "Unknown Time";
                var computer = lockResult.Lock?.ComputerName ?? "Unknown Device";

                var conflictDialog = new LockConflictDialog(lockOwner, lockTime, computer)
                {
                    Owner = this
                };

                conflictDialog.ShowDialog();

                if (conflictDialog.Result == LockConflictResult.ViewOnly)
                {
                    _isReadOnly = true;
                    TitleText.Text += " (View Only)";
                    SaveButton.Visibility = Visibility.Collapsed;
                    SaveNewButton.Visibility = Visibility.Collapsed;
                    DisableInputs();
                }
                else if (conflictDialog.Result == LockConflictResult.TakeOver)
                {
                    var takeover = await LockHelper.TakeOverLockAsync(_recordKey);
                    if (!takeover.Success)
                    {
                        DialogHelper.ShowError("Failed to take over editing lock. Switching to View Only.", "Lock Conflict");
                        _isReadOnly = true;
                        TitleText.Text += " (View Only)";
                        SaveButton.Visibility = Visibility.Collapsed;
                        SaveNewButton.Visibility = Visibility.Collapsed;
                        DisableInputs();
                    }
                    else
                    {
                        App.SignalRService.LockTakenOver += OnLockTakenOver;
                    }
                }
                else
                {
                    DialogResult = false;
                    Close();
                }
            }
            else
            {
                App.SignalRService.LockTakenOver += OnLockTakenOver;
            }
        }
    }

    private void OnLockTakenOver(string recordKey, string newOwner)
    {
        if (recordKey == _recordKey)
        {
            Dispatcher.Invoke(() =>
            {
                DialogHelper.ShowError($"Your editing session was taken over by {newOwner}. This window will now switch to View Only.", "Session Taken Over");
                _isReadOnly = true;
                TitleText.Text = TitleText.Text.Replace("Details", "Details (View Only)");
                SaveButton.Visibility = Visibility.Collapsed;
                SaveNewButton.Visibility = Visibility.Collapsed;
                DisableInputs();
            });
        }
    }

    private void DisableInputs()
    {
        MobileBox.IsEnabled = false;
        NameBox.IsEnabled = false;
        CompanyBox.IsEnabled = false;
        EmailBox.IsEnabled = false;
        ExistingAssetsCombo.IsEnabled = false;
        ProductNameBox.IsEnabled = false;
        BrandBox.IsEnabled = false;
        ModelBox.IsEnabled = false;
        SerialNumberBox.IsEnabled = false;
        ProblemBox.IsEnabled = false;
        PriorityCombo.IsEnabled = false;
        StatusCombo.IsEnabled = false;
        EngineerCombo.IsEnabled = false;
        RemarksBox.IsEnabled = false;
        SolutionBox.IsEnabled = false;
        UploadButton.IsEnabled = false;
        
        ChargerChk.IsEnabled = false;
        PowerCableChk.IsEnabled = false;
        BagChk.IsEnabled = false;
        BoxChk.IsEnabled = false;
        AdapterChk.IsEnabled = false;
        CaseChk.IsEnabled = false;
        OthersChk.IsEnabled = false;
        OthersTextBox.IsEnabled = false;
    }

    private void Input_Changed(object sender, EventArgs e)
    {
        if (_isLoaded)
        {
            _isDirty = true;
            ClearValidationError();
        }
    }

    private void ShowValidationError(string message)
    {
        ErrorBanner.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 238));
        ErrorBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 83, 80));
        ErrorBannerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40));
        BannerIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.AlertCircle;
        BannerIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 57, 53));
        ErrorBannerText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void ShowSuccessBanner(string message)
    {
        ErrorBanner.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
        ErrorBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 199, 132));
        ErrorBannerText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
        BannerIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircle;
        BannerIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
        ErrorBannerText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void ClearValidationError()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
    }

    private bool ValidateInputs()
    {
        ClearValidationError();
        
        if (string.IsNullOrWhiteSpace(MobileBox.Text))
        {
            ShowValidationError("Mobile Number is required.");
            MobileBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ShowValidationError("Customer Name is required.");
            NameBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProductNameBox.Text))
        {
            ShowValidationError("Product Name is required.");
            ProductNameBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProblemBox.Text))
        {
            ShowValidationError("Problem Description is required.");
            ProblemBox.Focus();
            return false;
        }

        return true;
    }

    private void ApplyInputsToDto()
    {
        ServiceEntry.CustomerMobile = MobileBox.Text.Trim();
        ServiceEntry.CustomerName = NameBox.Text.Trim();
        ServiceEntry.CustomerCompany = CompanyBox.Text.Trim();
        ServiceEntry.CustomerEmail = EmailBox.Text.Trim();

        ServiceEntry.ProductName = ProductNameBox.Text.Trim();
        ServiceEntry.ProductBrand = BrandBox.Text.Trim();
        ServiceEntry.ProductModel = ModelBox.Text.Trim();
        ServiceEntry.ProductSerialNumber = SerialNumberBox.Text.Trim();

        ServiceEntry.ProblemDescription = ProblemBox.Text.Trim();
        ServiceEntry.AccessoriesReceived = GetAccessoriesString();
        ServiceEntry.Remarks = RemarksBox.Text.Trim();
        ServiceEntry.Solution = SolutionBox.Text.Trim();

        var priorityItem = PriorityCombo.SelectedItem as ComboBoxItem;
        ServiceEntry.Priority = Enum.TryParse<ServiceEntryPriority>(priorityItem?.Tag?.ToString(), out var priority) ? priority : ServiceEntryPriority.Normal;

        var statusItem = StatusCombo.SelectedItem as ComboBoxItem;
        ServiceEntry.Status = Enum.TryParse<ServiceEntryStatus>(statusItem?.Tag?.ToString(), out var status) ? status : ServiceEntryStatus.Pending;

        ServiceEntry.AssignedToUserId = EngineerCombo.SelectedValue as int?;
        var selectedEng = EngineerCombo.SelectedItem as UserInfo;
        ServiceEntry.AssignedToUserName = selectedEng?.FullName;
    }

    private async Task<bool> SaveAsync()
    {
        if (_isReadOnly) return false;
        if (!ValidateInputs()) return false;

        ApplyInputsToDto();

        try
        {
            var dto = new ServiceEntrySaveDto
            {
                ServiceEntryId = ServiceEntry.ServiceEntryId > 0 ? ServiceEntry.ServiceEntryId : null,
                ServiceEntryNumber = ServiceEntry.ServiceEntryNumber,
                
                CustomerId = ServiceEntry.CustomerId,
                CustomerMobile = ServiceEntry.CustomerMobile,
                CustomerName = ServiceEntry.CustomerName,
                CustomerCompany = ServiceEntry.CustomerCompany,
                CustomerEmail = ServiceEntry.CustomerEmail,

                AssetId = ServiceEntry.ProductId,
                ProductName = ServiceEntry.ProductName,
                ProductBrand = ServiceEntry.ProductBrand,
                ProductModel = ServiceEntry.ProductModel,
                ProductSerialNumber = ServiceEntry.ProductSerialNumber,

                ProblemDescription = ServiceEntry.ProblemDescription,
                AccessoriesReceived = ServiceEntry.AccessoriesReceived,
                Remarks = ServiceEntry.Remarks,
                Solution = ServiceEntry.Solution,

                Status = ServiceEntry.Status,
                Priority = ServiceEntry.Priority,
                AssignedToUserId = ServiceEntry.AssignedToUserId
            };

            if (ServiceEntry.ServiceEntryId > 0)
            {
                await _apiService.PutAsync($"api/serviceentries/{ServiceEntry.ServiceEntryId}", dto);
            }
            else
            {
                var returned = await _apiService.PostAsync<ServiceEntrySaveDto, ServiceEntryDetailsDto>("api/serviceentries", dto);
                if (returned != null)
                {
                    ServiceEntry = returned;
                }
            }

            if (!string.IsNullOrEmpty(_recordKey))
            {
                await LockHelper.ReleaseLockAsync(_recordKey);
                _recordKey = string.Empty; // clear so Closing event doesn't duplicate
            }

            _isDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            ShowValidationError($"Unable to save service entry: {ex.Message}");
            return false;
        }
    }

    private async void SaveClose_Click(object sender, RoutedEventArgs e)
    {
        bool isNew = ServiceEntry.ServiceEntryId == 0;
        if (await SaveAsync())
        {
            ToastHelper.ShowToast(isNew ? "Service Entry Saved Successfully" : "Service Entry Updated Successfully");
            _isClosingFromSave = true;
            DialogResult = true;
            Close();
        }
    }

    private async void SaveNew_Click(object sender, RoutedEventArgs e)
    {
        if (await SaveAsync())
        {
            // Clear fields for next entry
            _isLoaded = false;
            
            MobileBox.Text = string.Empty;
            NameBox.Text = string.Empty;
            CompanyBox.Text = string.Empty;
            EmailBox.Text = string.Empty;

            ProductNameBox.Text = string.Empty;
            BrandBox.Text = string.Empty;
            ModelBox.Text = string.Empty;
            SerialNumberBox.Text = string.Empty;

            ProblemBox.Text = string.Empty;
            RemarksBox.Text = string.Empty;
            SolutionBox.Text = string.Empty;

            ChargerChk.IsChecked = false;
            PowerCableChk.IsChecked = false;
            BagChk.IsChecked = false;
            BoxChk.IsChecked = false;
            AdapterChk.IsChecked = false;
            CaseChk.IsChecked = false;
            OthersChk.IsChecked = false;
            OthersTextBox.Text = string.Empty;
            OthersTextBox.Visibility = Visibility.Collapsed;

            ExistingAssetsCombo.Visibility = Visibility.Collapsed;
            ExistingAssetsCombo.ItemsSource = null;

            AttachmentsGrid.Visibility = Visibility.Collapsed;
            AttachmentsNote.Visibility = Visibility.Visible;
            UploadButton.IsEnabled = false;

            ServiceEntry = new ServiceEntryDetailsDto();
            _isDirty = false;
            
            ShowSuccessBanner("Service entry saved successfully. Ready for the next one!");
            
            // Re-focus Mobile number
            MobileBox.Focus();
            
            _isLoaded = true;
            
            // Trigger refresh on parent
            DialogResult = true;
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_isReadOnly) return;
        _isLoaded = false;

        MobileBox.Text = _initialServiceEntry.CustomerMobile;
        NameBox.Text = _initialServiceEntry.CustomerName;
        CompanyBox.Text = _initialServiceEntry.CustomerCompany;
        EmailBox.Text = _initialServiceEntry.CustomerEmail;

        ProductNameBox.Text = _initialServiceEntry.ProductName;
        BrandBox.Text = _initialServiceEntry.ProductBrand;
        ModelBox.Text = _initialServiceEntry.ProductModel;
        SerialNumberBox.Text = _initialServiceEntry.ProductSerialNumber;

        ProblemBox.Text = _initialServiceEntry.ProblemDescription;
        RemarksBox.Text = _initialServiceEntry.Remarks;
        SolutionBox.Text = _initialServiceEntry.Solution;

        LoadAccessories(_initialServiceEntry.AccessoriesReceived);

        foreach (ComboBoxItem item in PriorityCombo.Items)
        {
            if (item.Tag?.ToString() == _initialServiceEntry.Priority.ToString())
            {
                item.IsSelected = true;
                break;
            }
        }

        foreach (ComboBoxItem item in StatusCombo.Items)
        {
            if (item.Tag?.ToString() == _initialServiceEntry.Status.ToString())
            {
                item.IsSelected = true;
                break;
            }
        }

        EngineerCombo.SelectedValue = _initialServiceEntry.AssignedToUserId;

        ServiceEntry.Attachments = _initialServiceEntry.Attachments.ToList();
        RefreshAttachmentsGrid();

        _isDirty = false;
        ClearValidationError();
        _isLoaded = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosingFromSave) return;

        if (_isDirty)
        {
            var result = DialogHelper.ConfirmSaveDiscardCancel("You have unsaved changes. Do you want to save them before closing?", "Unsaved Changes");
            if (result == ModernMessageResult.Save)
            {
                e.Cancel = true;
                if (await SaveAsync())
                {
                    _isClosingFromSave = true;
                    DialogResult = true;
                    Close();
                }
            }
            else if (result == ModernMessageResult.Discard)
            {
                if (!string.IsNullOrEmpty(_recordKey) && !_isReadOnly)
                {
                    await LockHelper.ReleaseLockAsync(_recordKey);
                }
            }
            else // Cancel
            {
                e.Cancel = true;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_recordKey) && !_isReadOnly)
            {
                await LockHelper.ReleaseLockAsync(_recordKey);
            }
        }
    }

    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.S && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            e.Handled = true;
            if (await SaveAsync())
            {
                _isClosingFromSave = true;
                DialogResult = true;
                Close();
            }
        }
        else if (e.Key == System.Windows.Input.Key.N && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            e.Handled = true;
            SaveNew_Click(sender, new RoutedEventArgs());
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        App.SignalRService.LockTakenOver -= OnLockTakenOver;
        _busyScopeValue.Dispose();
        base.OnClosed(e);
    }

    private void OthersChk_Checked(object sender, RoutedEventArgs e)
    {
        if (OthersTextBox != null)
        {
            OthersTextBox.Visibility = OthersChk.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        Input_Changed(sender, e);
    }

    private void Template_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string templateText)
        {
            if (string.IsNullOrWhiteSpace(ProblemBox.Text))
            {
                ProblemBox.Text = templateText;
            }
            else
            {
                ProblemBox.Text += Environment.NewLine + templateText;
            }
            ProblemBox.Focus();
            ProblemBox.CaretIndex = ProblemBox.Text.Length;
        }
    }

    private async void MobileBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Input_Changed(sender, e);
        if (!_isLoaded) return;

        _debounceCts?.Cancel();
        _debounceCts = new System.Threading.CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(350, token);
            if (token.IsCancellationRequested) return;

            var rawInput = MobileBox.Text.Trim();
            var digitsInput = new string(rawInput.Where(char.IsDigit).ToArray());
            if (digitsInput.Length >= 5)
            {
                var match = _allCustomers.FirstOrDefault(c => 
                {
                    var cleanMobile = new string(c.Mobile.Where(char.IsDigit).ToArray());
                    return cleanMobile.Contains(digitsInput) || cleanMobile == digitsInput;
                });

                if (match != null)
                {
                    NameBox.Text = match.CustomerName;
                    CompanyBox.Text = match.Company;
                    EmailBox.Text = match.Email;

                    await LoadAssetsForCustomerAsync(match.CustomerId);
                }
                else
                {
                    ExistingAssetsCombo.Visibility = Visibility.Collapsed;
                    ExistingAssetsCombo.ItemsSource = null;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task LoadAssetsForCustomerAsync(int customerId)
    {
        try
        {
            var assets = await _apiService.GetAsync<List<AssetLookupItem>>($"api/assets/by-customer/{customerId}");
            if (assets != null && assets.Any())
            {
                ExistingAssetsCombo.ItemsSource = assets;
                ExistingAssetsCombo.Visibility = Visibility.Visible;
            }
            else
            {
                ExistingAssetsCombo.Visibility = Visibility.Collapsed;
                ExistingAssetsCombo.ItemsSource = null;
            }
        }
        catch (Exception)
        {
        }
    }

    private async void ExistingAssetsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ExistingAssetsCombo.SelectedValue is int assetId)
        {
            try
            {
                var asset = await _apiService.GetAsync<AssetDto>($"api/assets/{assetId}");
                if (asset != null)
                {
                    ProductNameBox.Text = asset.ProductName;
                    BrandBox.Text = asset.Brand;
                    ModelBox.Text = asset.Model;
                    SerialNumberBox.Text = asset.SerialNumber;
                }
            }
            catch (Exception)
            {
                ShowValidationError("Unable to load product details.");
            }
        }
    }

    private async void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        if (ServiceEntry.ServiceEntryId <= 0) return;

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select File to Upload",
            Filter = "All Files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var returnedDto = await _apiService.UploadFileAsync<ServiceEntryAttachmentDto>(
                    $"api/serviceentries/{ServiceEntry.ServiceEntryId}/attachments",
                    openFileDialog.FileName);

                if (returnedDto != null)
                {
                    ServiceEntry.Attachments.Add(returnedDto);
                    RefreshAttachmentsGrid();
                    ShowSuccessBanner("File uploaded successfully.");
                }
            }
            catch (Exception ex)
            {
                ShowValidationError($"File upload failed: {ex.Message}");
            }
        }
    }

    private async void DownloadAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ServiceEntryAttachmentDto attachment)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Download Attachment",
                FileName = attachment.FileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fileBytes = await _apiService.GetByteArrayAsync($"api/serviceentries/attachments/{attachment.Id}");
                    await System.IO.File.WriteAllBytesAsync(saveFileDialog.FileName, fileBytes);
                    ShowSuccessBanner("File downloaded successfully.");
                }
                catch (Exception ex)
                {
                    ShowValidationError($"Download failed: {ex.Message}");
                }
            }
        }
    }

    private async void DeleteAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ServiceEntryAttachmentDto attachment)
        {
            var confirm = DialogHelper.Confirm($"Are you sure you want to delete '{attachment.FileName}'?",
                "Confirm Delete");

            if (confirm)
            {
                try
                {
                    await _apiService.DeleteAsync($"api/serviceentries/attachments/{attachment.Id}");
                    ServiceEntry.Attachments.Remove(attachment);
                    RefreshAttachmentsGrid();
                    ShowSuccessBanner("Attachment deleted successfully.");
                }
                catch (Exception ex)
                {
                    ShowValidationError($"Delete failed: {ex.Message}");
                }
            }
        }
    }

    private void RefreshAttachmentsGrid()
    {
        AttachmentsGrid.ItemsSource = null;
        AttachmentsGrid.ItemsSource = ServiceEntry.Attachments;
        if (ServiceEntry.Attachments.Any())
        {
            AttachmentsGrid.Visibility = Visibility.Visible;
            AttachmentsNote.Visibility = Visibility.Collapsed;
        }
        else
        {
            AttachmentsGrid.Visibility = Visibility.Collapsed;
            AttachmentsNote.Visibility = Visibility.Visible;
        }
    }

    private void LoadAccessories(string? accessoriesString)
    {
        if (string.IsNullOrEmpty(accessoriesString)) return;

        var list = accessoriesString.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        ChargerChk.IsChecked = list.Contains("Charger");
        PowerCableChk.IsChecked = list.Contains("Power Cable");
        BagChk.IsChecked = list.Contains("Bag");
        BoxChk.IsChecked = list.Contains("Box");
        AdapterChk.IsChecked = list.Contains("Adapter");
        CaseChk.IsChecked = list.Contains("Case");

        var standard = new HashSet<string> { "Charger", "Power Cable", "Bag", "Box", "Adapter", "Case" };
        var otherItems = list.Where(item => !standard.Contains(item)).ToList();
        if (otherItems.Any())
        {
            OthersChk.IsChecked = true;
            OthersTextBox.Visibility = Visibility.Visible;
            OthersTextBox.Text = string.Join(", ", otherItems);
        }
    }

    private string GetAccessoriesString()
    {
        var list = new List<string>();
        if (ChargerChk.IsChecked == true) list.Add("Charger");
        if (PowerCableChk.IsChecked == true) list.Add("Power Cable");
        if (BagChk.IsChecked == true) list.Add("Bag");
        if (BoxChk.IsChecked == true) list.Add("Box");
        if (AdapterChk.IsChecked == true) list.Add("Adapter");
        if (CaseChk.IsChecked == true) list.Add("Case");

        if (OthersChk.IsChecked == true && !string.IsNullOrWhiteSpace(OthersTextBox.Text))
        {
            list.Add(OthersTextBox.Text.Trim());
        }

        return string.Join(", ", list);
    }

    // Helper classes

    public class CustomerDetails
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string? Email { get; set; }
    }

    public class AssetDto
    {
        public int AssetId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
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
    public class ServiceEntrySaveDto
    {
        public int? ServiceEntryId { get; set; }
        public string? ServiceEntryNumber { get; set; }
        
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
        public string? CustomerCompany { get; set; }
        public string? CustomerEmail { get; set; }

        public int AssetId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductBrand { get; set; }
        public string? ProductModel { get; set; }
        public string? ProductSerialNumber { get; set; }

        public string ProblemDescription { get; set; } = string.Empty;
        public string? AccessoriesReceived { get; set; }
        public string? Remarks { get; set; }
        public string? Solution { get; set; }

        public ServiceEntryStatus Status { get; set; }
        public ServiceEntryPriority Priority { get; set; }
        public int? AssignedToUserId { get; set; }
    }
}
