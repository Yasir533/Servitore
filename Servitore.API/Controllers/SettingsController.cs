using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActivityLogService _activityLogService;

    public SettingsController(AppDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settingsList = await _context.Settings.ToListAsync();
        var dto = new SettingsDto
        {
            CompanyName = settingsList.FirstOrDefault(s => s.Key == "CompanyName")?.Value,
            CompanyPhone = settingsList.FirstOrDefault(s => s.Key == "CompanyPhone")?.Value,
            CompanyEmail = settingsList.FirstOrDefault(s => s.Key == "CompanyEmail")?.Value,
            CompanyWebsite = settingsList.FirstOrDefault(s => s.Key == "CompanyWebsite")?.Value,
            CompanyAddress = settingsList.FirstOrDefault(s => s.Key == "CompanyAddress")?.Value,
            SmtpHost = settingsList.FirstOrDefault(s => s.Key == "SmtpHost")?.Value,
            SmtpPort = int.TryParse(settingsList.FirstOrDefault(s => s.Key == "SmtpPort")?.Value, out var port) ? port : null,
            SmtpFromAddress = settingsList.FirstOrDefault(s => s.Key == "SmtpFromAddress")?.Value,
            SmtpFromName = settingsList.FirstOrDefault(s => s.Key == "SmtpFromName")?.Value,
            SmtpUsername = settingsList.FirstOrDefault(s => s.Key == "SmtpUsername")?.Value,
            TicketNumberFormat = settingsList.FirstOrDefault(s => s.Key == "TicketNumberFormat")?.Value
        };
        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings(SettingsDto dto)
    {
        var settingsList = await _context.Settings.ToListAsync();

        void SaveSetting(string key, string? value)
        {
            var existing = settingsList.FirstOrDefault(s => s.Key == key);
            if (existing is null)
            {
                if (value != null)
                {
                    _context.Settings.Add(new Settings { Key = key, Value = value });
                }
            }
            else
            {
                if (value != null)
                {
                    existing.Value = value;
                }
                else
                {
                    _context.Settings.Remove(existing);
                }
            }
        }

        SaveSetting("CompanyName", dto.CompanyName);
        SaveSetting("CompanyPhone", dto.CompanyPhone);
        SaveSetting("CompanyEmail", dto.CompanyEmail);
        SaveSetting("CompanyWebsite", dto.CompanyWebsite);
        SaveSetting("CompanyAddress", dto.CompanyAddress);
        SaveSetting("SmtpHost", dto.SmtpHost);
        SaveSetting("SmtpPort", dto.SmtpPort?.ToString());
        SaveSetting("SmtpFromAddress", dto.SmtpFromAddress);
        SaveSetting("SmtpFromName", dto.SmtpFromName);
        SaveSetting("SmtpUsername", dto.SmtpUsername);
        SaveSetting("TicketNumberFormat", dto.TicketNumberFormat);

        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync("Updated system settings", "Settings", HttpContext);
        return NoContent();
    }

    [HttpGet("whatsapp")]
    public async Task<IActionResult> GetWhatsAppSettings() =>
        Ok(await _context.WhatsAppSettings.FirstOrDefaultAsync());

    [HttpPut("whatsapp")]
    public async Task<IActionResult> UpdateWhatsAppSettings(WhatsAppSettings settings)
    {
        var existing = await _context.WhatsAppSettings.FirstOrDefaultAsync();
        if (existing is null)
        {
            _context.WhatsAppSettings.Add(settings);
        }
        else
        {
            existing.PhoneNumber = settings.PhoneNumber;
            existing.ApiKey = settings.ApiKey;
            existing.IsEnabled = settings.IsEnabled;
        }

        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Updated WhatsApp Settings (Phone: {settings.PhoneNumber}, Enabled: {settings.IsEnabled})", "Settings", HttpContext);
        return NoContent();
    }
}

public class SettingsDto
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
