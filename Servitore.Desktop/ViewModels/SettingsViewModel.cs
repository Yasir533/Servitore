using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ApiService _apiService;

    [ObservableProperty] private string companyName = string.Empty;
    [ObservableProperty] private string companyPhone = string.Empty;
    [ObservableProperty] private string companyEmail = string.Empty;
    [ObservableProperty] private string companyWebsite = string.Empty;
    [ObservableProperty] private string companyAddress = string.Empty;

    [ObservableProperty] private string smtpHost = string.Empty;
    [ObservableProperty] private string smtpPort = "587";
    [ObservableProperty] private string smtpFromAddress = string.Empty;
    [ObservableProperty] private string smtpFromName = string.Empty;
    [ObservableProperty] private string smtpUsername = string.Empty;

    [ObservableProperty] private string ticketNumberFormat = "TKT-{YYYY}-{0000}";

    [ObservableProperty] private string waPhoneNumber = string.Empty;
    [ObservableProperty] private string waApiKey = string.Empty;
    [ObservableProperty] private bool waIsEnabled;

    public SettingsViewModel(ApiService apiService) => _apiService = apiService;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            int maxRetries = 15;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var settings = await _apiService.GetAsync<SettingsDto>("api/settings");
                    if (settings is not null)
                    {
                        CompanyName    = settings.CompanyName ?? string.Empty;
                        CompanyPhone   = settings.CompanyPhone ?? string.Empty;
                        CompanyEmail   = settings.CompanyEmail ?? string.Empty;
                        CompanyWebsite = settings.CompanyWebsite ?? string.Empty;
                        CompanyAddress = settings.CompanyAddress ?? string.Empty;

                        SmtpHost        = settings.SmtpHost ?? string.Empty;
                        SmtpPort        = settings.SmtpPort?.ToString() ?? "587";
                        SmtpFromAddress = settings.SmtpFromAddress ?? string.Empty;
                        SmtpFromName    = settings.SmtpFromName ?? string.Empty;
                        SmtpUsername    = settings.SmtpUsername ?? string.Empty;

                        TicketNumberFormat = settings.TicketNumberFormat ?? "TKT-{YYYY}-{0000}";
                    }
                    break; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load settings failed", ex);
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fail-safe
        }

        try
        {
            int maxRetries = 15;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var waSettings = await _apiService.GetAsync<WhatsAppSettingsDto>("api/settings/whatsapp");
                    if (waSettings is not null)
                    {
                        WaPhoneNumber = waSettings.PhoneNumber ?? string.Empty;
                        WaApiKey      = waSettings.ApiKey ?? string.Empty;
                        WaIsEnabled   = waSettings.IsEnabled;
                    }
                    break; // Success!
                }
                catch (Exception ex)
                {
                    Helpers.ClientLogger.Log($"Attempt {i + 1} to load WhatsApp settings failed", ex);
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fail-safe
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var dto = new SettingsDto
            {
                CompanyName    = CompanyName,
                CompanyPhone   = CompanyPhone,
                CompanyEmail   = CompanyEmail,
                CompanyWebsite = CompanyWebsite,
                CompanyAddress = CompanyAddress,
                SmtpHost       = SmtpHost,
                SmtpPort       = int.TryParse(SmtpPort, out var p) ? p : 587,
                SmtpFromAddress = SmtpFromAddress,
                SmtpFromName   = SmtpFromName,
                SmtpUsername   = SmtpUsername,
                TicketNumberFormat = TicketNumberFormat
            };
            await _apiService.PutAsync("api/settings", dto);

            var waDto = new WhatsAppSettingsDto
            {
                PhoneNumber = WaPhoneNumber,
                ApiKey = WaApiKey,
                IsEnabled = WaIsEnabled
            };
            await _apiService.PutAsync("api/settings/whatsapp", waDto);

            Helpers.DialogHelper.ShowInfo("Settings saved successfully.");
        }
        catch (Exception)
        {
            Helpers.DialogHelper.ShowError("Unable to save changes. Please try again later.");
        }
    }

    private class SettingsDto
    {
        public string? CompanyName { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyAddress { get; set; }
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpFromAddress { get; set; }
        public string? SmtpFromName { get; set; }
        public string? SmtpUsername { get; set; }
        public string? TicketNumberFormat { get; set; }
    }

    private class WhatsAppSettingsDto
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
