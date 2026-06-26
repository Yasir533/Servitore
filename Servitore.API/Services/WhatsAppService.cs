using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Servitore.API.Services;

public interface IWhatsAppService
{
    Task SendNotificationAsync(string username, string action, string recordName);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly AppDbContext _context;

    public WhatsAppService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<WhatsAppService> _loggerVal,
        AppDbContext context)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = _loggerVal;
        _context = context;
    }

    public async Task SendNotificationAsync(string username, string action, string recordName)
    {
        var localTime = DateTime.Now;
        var date = localTime.ToString("yyyy-MM-dd");
        var time = localTime.ToString("HH:mm:ss");
        var message = $"User: {username}, Action: {action}, Record Name: {recordName}, Date: {date}, Time: {time}";
        await SendAsync(message);
    }

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

        // Broadcast to all comma-separated numbers
        var numbers = companyNumber.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var number in numbers)
        {
            var payload = new
            {
                to = number,
                message
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = JsonContent(payload);
                await _httpClient.SendAsync(request);
                _logger.LogInformation("Successfully sent WhatsApp notification to {Number}", number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp notification to {Number}.", number);
            }
        }
    }

    private static HttpContent JsonContent(object payload) =>
        new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
}
