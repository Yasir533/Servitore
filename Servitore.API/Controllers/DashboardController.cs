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

        var todayEntriesCount = await _context.ServiceEntries
            .CountAsync(t => t.CreatedDate.Date == today);

        var onlineUsers = Servitore.API.SignalR.PresenceManager.GetConnectedUsers()
            .Select(u => u.Username)
            .Distinct()
            .Count();

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

        // 4. Quick View metrics
        var quickViewServiceCalls = await _context.ServiceEntries.CountAsync(t => !t.IsDeleted);
        var quickViewPendingCalls = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewDeadlineCalls = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.IsTomorrow && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewPriorityCalls = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && (t.Priority == ServiceEntryPriority.High || t.Priority == ServiceEntryPriority.Urgent) && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewRegisteredClosedToday = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.CreatedDate.Date == today)
            + await _context.ServiceEntries.CountAsync(t => !t.IsDeleted && (t.Status == ServiceEntryStatus.Completed || t.Status == ServiceEntryStatus.Delivered) && t.ModifiedDate != null && t.ModifiedDate.Value.Date == today);
        var quickViewPendingCustomerResponse = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.SubCallType == "Pending for Customer Response" && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewPendingSpare = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.SubCallType == "Pending for Spare" && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewPendingTechnicalSupport = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.SubCallType == "Pending for Technical Support" && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewPendingOthers = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.SubCallType == "Pending for Others Reason" && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewItemNotDelivered = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.Status != ServiceEntryStatus.Delivered && t.ServiceType == "InHouse");
        var quickViewUnassignedCalls = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.AssignedToUserId == null && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewTransferRequested = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.SubCallType == "Transfer Requested" && (t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress));
        var quickViewDeliveryChallan = await _context.ServiceEntries
            .CountAsync(t => !t.IsDeleted && t.Status == ServiceEntryStatus.Delivered);

        var summary = new Servitore.Shared.Models.DashboardSummary
        {
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            OpenServiceEntries = openEntriesCount,
            ServiceEntriesResolvedToday = entriesResolvedToday,
            TodayServiceEntries = todayEntriesCount,
            OnlineUsers = onlineUsers,
            RecentNotifications = recentNotifications,
            RecentServiceEntries = recentEntries,
            ServiceEntryStatusCounts = entryStatusCounts,
            RecentActivities = recentActivities,

            QuickViewServiceCalls = quickViewServiceCalls,
            QuickViewPendingCalls = quickViewPendingCalls,
            QuickViewDeadlineCalls = quickViewDeadlineCalls,
            QuickViewPriorityCalls = quickViewPriorityCalls,
            QuickViewRegisteredClosedToday = quickViewRegisteredClosedToday,
            QuickViewPendingCustomerResponse = quickViewPendingCustomerResponse,
            QuickViewPendingSpare = quickViewPendingSpare,
            QuickViewPendingTechnicalSupport = quickViewPendingTechnicalSupport,
            QuickViewPendingOthers = quickViewPendingOthers,
            QuickViewItemNotDelivered = quickViewItemNotDelivered,
            QuickViewUnassignedCalls = quickViewUnassignedCalls,
            QuickViewTransferRequested = quickViewTransferRequested,
            QuickViewDeliveryChallan = quickViewDeliveryChallan
        };

        return Ok(summary);
    }
}
