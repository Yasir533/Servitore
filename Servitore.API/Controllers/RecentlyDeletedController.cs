using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Servitore.API.SignalR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class RecentlyDeletedController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<CollaborationHub> _hubContext;
    private readonly IActivityLogService _activityLogService;

    public RecentlyDeletedController(AppDbContext context, IHubContext<CollaborationHub> hubContext, IActivityLogService activityLogService)
    {
        _context = context;
        _hubContext = hubContext;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var retentionSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "RecentlyDeletedRetentionDays");
        int retentionDays = int.TryParse(retentionSetting?.Value, out var rdDays) ? rdDays : 10;
        var now = DateTime.UtcNow;

        var items = new List<RecentlyDeletedItemDto>();

        // 1. Customers
        var customers = await _context.Customers
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .Select(c => new RecentlyDeletedItemDto
            {
                Id = c.CustomerId,
                Type = "Customer",
                Name = c.CustomerName,
                DeletedBy = c.DeletedBy ?? "Unknown",
                DeletedDate = c.DeletedDate ?? c.CreatedDate
            })
            .ToListAsync();

        // 2. Products (Assets)
        var assets = await _context.Assets
            .IgnoreQueryFilters()
            .Where(a => a.IsDeleted)
            .Select(a => new RecentlyDeletedItemDto
            {
                Id = a.AssetId,
                Type = "Product",
                Name = a.ProductName + " (" + a.AssetCode + ")",
                DeletedBy = a.DeletedBy ?? "Unknown",
                DeletedDate = a.DeletedDate ?? a.CreatedDate
            })
            .ToListAsync();

        // 3. Service Entries
        var entries = await _context.ServiceEntries
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .Select(e => new RecentlyDeletedItemDto
            {
                Id = e.ServiceEntryId,
                Type = "ServiceEntry",
                Name = e.ServiceEntryNumber + " - " + e.ProblemDescription,
                DeletedBy = e.DeletedBy ?? "Unknown",
                DeletedDate = e.DeletedDate ?? e.CreatedDate
            })
            .ToListAsync();

        items.AddRange(customers);
        items.AddRange(assets);
        items.AddRange(entries);

        // Map DaysRemaining
        foreach (var item in items)
        {
            var elapsedDays = (now - item.DeletedDate).Days;
            item.DaysRemaining = Math.Max(0, retentionDays - elapsedDays);
        }

        return Ok(items.OrderByDescending(i => i.DeletedDate));
    }

    [HttpPost("restore/{type}/{id:int}")]
    public async Task<IActionResult> Restore(string type, int id)
    {
        if (type == "Customer")
        {
            var item = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (item != null)
            {
                item.IsDeleted = false;
                item.DeletedDate = null;
                item.DeletedBy = null;
                await _context.SaveChangesAsync();
                await _activityLogService.LogActivityAsync($"Customer Restored: {item.CustomerName} (ID: {id})", "Customers", HttpContext);
            }
        }
        else if (type == "Product")
        {
            var item = await _context.Assets.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AssetId == id);
            if (item != null)
            {
                item.IsDeleted = false;
                item.DeletedDate = null;
                item.DeletedBy = null;
                await _context.SaveChangesAsync();
                await _activityLogService.LogActivityAsync($"Product Restored: {item.ProductName} (ID: {id})", "Assets", HttpContext);
            }
        }
        else if (type == "ServiceEntry")
        {
            var item = await _context.ServiceEntries.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.ServiceEntryId == id);
            if (item != null)
            {
                item.IsDeleted = false;
                item.DeletedDate = null;
                item.DeletedBy = null;
                await _context.SaveChangesAsync();
                await _activityLogService.LogActivityAsync($"Service Entry Restored: {item.ServiceEntryNumber} (ID: {id})", "ServiceEntries", HttpContext);
            }
        }
        else
        {
            return BadRequest("Invalid type");
        }

        // Broadcast data change to sync all clients
        await _hubContext.Clients.All.SendAsync("DataChanged", new Servitore.Shared.Models.DataEventModel
        {
            EntityType = type,
            Action = "Restored",
            RecordId = id.ToString(),
            Username = User.Identity?.Name ?? "Unknown"
        });

        return NoContent();
    }

    [HttpDelete("permanent/{type}/{id:int}")]
    public async Task<IActionResult> DeletePermanent(string type, int id)
    {
        if (type == "Customer")
        {
            var item = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (item != null)
            {
                _context.Customers.Remove(item);
                await _context.SaveChangesAsync();
                await _activityLogService.LogActivityAsync($"Customer Permanently Deleted ID: {id}", "Customers", HttpContext);
            }
        }
        else if (type == "Product")
        {
            var item = await _context.Assets.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AssetId == id);
            if (item != null)
            {
                _context.Assets.Remove(item);
                await _context.SaveChangesAsync();
                await _activityLogService.LogActivityAsync($"Product Permanently Deleted ID: {id}", "Assets", HttpContext);
            }
        }
        else if (type == "ServiceEntry")
        {
            var item = await _context.ServiceEntries.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.ServiceEntryId == id);
            if (item != null)
            {
                _context.ServiceEntries.Remove(item);
                await _context.SaveChangesAsync();
                await _activityLogService.LogActivityAsync($"Service Entry Permanently Deleted ID: {id}", "ServiceEntries", HttpContext);
            }
        }
        else
        {
            return BadRequest("Invalid type");
        }

        // Broadcast data change to sync all clients
        await _hubContext.Clients.All.SendAsync("DataChanged", new Servitore.Shared.Models.DataEventModel
        {
            EntityType = type,
            Action = "PermanentlyDeleted",
            RecordId = id.ToString(),
            Username = User.Identity?.Name ?? "Unknown"
        });

        return NoContent();
    }
}

public class RecentlyDeletedItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeletedBy { get; set; } = string.Empty;
    public DateTime DeletedDate { get; set; }
    public int DaysRemaining { get; set; }
}
