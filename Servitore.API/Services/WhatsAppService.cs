using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.API.Services;

public interface IWhatsAppService
{
    Task SendTicketCreatedAsync(ServiceTicket ticket);
    Task SendTicketUpdatedAsync(ServiceTicket ticket);
    Task SendTicketCompletedAsync(ServiceTicket ticket);
}

// Sends WhatsApp messages to the configured company number whenever a ticket
// is created, updated, or completed. Wire ApiUrl/ApiKey/CompanyNumber in
// appsettings ("WhatsApp" section) or via Settings database table.
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly AppDbContext _context;

    public WhatsAppService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<WhatsAppService> logger,
        AppDbContext context)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    public Task SendTicketCreatedAsync(ServiceTicket ticket) =>
        SendAsync($"New ticket {ticket.TicketNumber} created.");

    public Task SendTicketUpdatedAsync(ServiceTicket ticket) =>
        SendAsync($"Ticket {ticket.TicketNumber} updated. Status: {ticket.Status}.");

    public Task SendTicketCompletedAsync(ServiceTicket ticket) =>
        SendAsync($"Ticket {ticket.TicketNumber} completed.");

    private async Task SendAsync(string message)
    {
        WhatsAppSettings? dbSettings = null;
        try
        {
            dbSettings = await _context.WhatsAppSettings.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read WhatsApp settings from database.");
        }

        bool isEnabled;
        string? apiUrl;
        string? apiKey;
        string? companyNumber;

        if (dbSettings is not null)
        {
            isEnabled = dbSettings.IsEnabled;
            apiUrl = _configuration["WhatsApp:ApiUrl"];
            apiKey = dbSettings.ApiKey;
            companyNumber = dbSettings.PhoneNumber;
        }
        else
        {
            // Fallback to configuration
            isEnabled = !string.IsNullOrWhiteSpace(_configuration["WhatsApp:ApiKey"]);
            apiUrl = _configuration["WhatsApp:ApiUrl"];
            apiKey = _configuration["WhatsApp:ApiKey"];
            companyNumber = _configuration["WhatsApp:CompanyNumber"];
        }

        if (!isEnabled || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(companyNumber))
        {
            _logger.LogInformation("[WhatsApp disabled or unconfigured] Would send: {Message}", message);
            return;
        }

        var payload = new
        {
            to = companyNumber,
            message
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent(payload);
            await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp notification.");
        }
    }

    private static HttpContent JsonContent(object payload) =>
        new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
}
