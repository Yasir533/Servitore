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
    public async Task<IActionResult> GetAll() => Ok(await _context.Settings.ToListAsync());

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
