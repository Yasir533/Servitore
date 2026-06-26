using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Shared.Enums;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.UtcNow.Date;
        var in30Days = today.AddDays(30);

        var totalCustomers = await _context.Customers.CountAsync();
        var totalProducts = await _context.Assets.CountAsync();
        
        var openEntriesCount = await _context.ServiceEntries
            .CountAsync(t => t.Status == ServiceEntryStatus.Pending ||
                             t.Status == ServiceEntryStatus.InProgress);

        var entriesResolvedToday = await _context.ServiceEntries
            .CountAsync(t => (t.Status == ServiceEntryStatus.Completed ||
                              t.Status == ServiceEntryStatus.Delivered) &&
                             t.CreatedDate.Date == today);

        var recentNotifications = await _context.Notifications
            .OrderByDescending(n => n.CreatedDate)
            .Take(10)
            .Select(n => new Servitore.Shared.Models.NotificationModel 
            { 
                Message = n.Message, 
                CreatedDate = n.CreatedDate,
                Type = n.Type,
                CreatedBy = n.CreatedBy
            })
            .ToListAsync();

        // 1. Recent Service Entries
        var recentEntries = await _context.ServiceEntries
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .OrderByDescending(t => t.CreatedDate)
            .Take(5)
            .Select(t => new Servitore.Shared.Models.DashboardServiceEntryDto
            {
                ServiceEntryId = t.ServiceEntryId,
                ServiceEntryNumber = t.ServiceEntryNumber,
                CustomerName = t.Customer != null ? t.Customer.CustomerName : string.Empty,
                ProductName = t.Asset != null ? t.Asset.ProductName : string.Empty,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                CreatedDate = t.CreatedDate
            })
            .ToListAsync();

        // 2. Service Entry Status Counts
        var allEntryStatuses = await _context.ServiceEntries
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var entryStatusCounts = new Dictionary<string, int>();
        foreach (var status in Enum.GetValues<ServiceEntryStatus>())
        {
            entryStatusCounts[status.ToString()] = 0;
        }
        foreach (var item in allEntryStatuses)
        {
            entryStatusCounts[item.Status.ToString()] = item.Count;
        }

        // 3. Recent Activities
        var recentActivities = await _context.ActivityLogs
            .OrderByDescending(a => a.DateTime)
            .Take(10)
            .Select(a => new Servitore.Shared.Models.ActivityLogDto
            {
                Id = a.Id,
                LogId = a.LogId,
                Action = a.Action,
                Module = a.Module,
                UserId = a.UserId,
                UserName = a.UserName,
                SystemName = a.SystemName,
                IPAddress = a.IPAddress,
                DateTime = a.DateTime
            })
            .ToListAsync();

        var summary = new Servitore.Shared.Models.DashboardSummary
        {
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            OpenServiceEntries = openEntriesCount,
            ServiceEntriesResolvedToday = entriesResolvedToday,
            RecentNotifications = recentNotifications,
            RecentServiceEntries = recentEntries,
            ServiceEntryStatusCounts = entryStatusCounts,
            RecentActivities = recentActivities
        };

        return Ok(summary);
    }
}
