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
        var totalAssets = await _context.Assets.CountAsync();
        
        var openTicketsCount = await _context.ServiceTickets
            .CountAsync(t => t.Status == TicketStatus.Open ||
                             t.Status == TicketStatus.InProgress ||
                             t.Status == TicketStatus.OnHold);

        var ticketsResolvedToday = await _context.ServiceTickets
            .CountAsync(t => (t.Status == TicketStatus.Resolved ||
                              t.Status == TicketStatus.Closed) &&
                             t.CreatedDate.Date == today);

        var expiringWarrantiesCount = await _context.Warranties
            .CountAsync(w => w.EndDate >= today && w.EndDate <= in30Days);

        var expiringAmcContractsCount = await _context.AMCContracts
            .CountAsync(a => a.EndDate >= today && a.EndDate <= in30Days);

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

        // 1. Recent Tickets
        var recentTickets = await _context.ServiceTickets
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .OrderByDescending(t => t.CreatedDate)
            .Take(5)
            .Select(t => new Servitore.Shared.Models.DashboardTicketDto
            {
                TicketId = t.TicketId,
                TicketNumber = t.TicketNumber,
                CustomerName = t.Customer != null ? t.Customer.CustomerName : string.Empty,
                AssetName = t.Asset != null ? t.Asset.ProductName : string.Empty,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                CreatedDate = t.CreatedDate
            })
            .ToListAsync();

        // 2. AMC Alerts
        var amcAlerts = await _context.AMCContracts
            .Include(a => a.Asset)
            .ThenInclude(ast => ast.Customer)
            .Where(a => a.EndDate >= today && a.EndDate <= in30Days)
            .Select(a => new Servitore.Shared.Models.DashboardAmcAlertDto
            {
                AMCContractId = a.AMCContractId,
                AssetName = a.Asset != null ? a.Asset.ProductName : string.Empty,
                CustomerName = (a.Asset != null && a.Asset.Customer != null) ? a.Asset.Customer.CustomerName : string.Empty,
                EndDate = a.EndDate,
                DaysRemaining = (a.EndDate - today).Days,
                ContractValue = a.ContractValue
            })
            .ToListAsync();

        // 3. Warranty Alerts
        var warrantyAlerts = await _context.Warranties
            .Include(w => w.Asset)
            .ThenInclude(ast => ast.Customer)
            .Where(w => w.EndDate >= today && w.EndDate <= in30Days)
            .Select(w => new Servitore.Shared.Models.DashboardWarrantyAlertDto
            {
                WarrantyId = w.WarrantyId,
                AssetName = w.Asset != null ? w.Asset.ProductName : string.Empty,
                CustomerName = (w.Asset != null && w.Asset.Customer != null) ? w.Asset.Customer.CustomerName : string.Empty,
                EndDate = w.EndDate,
                DaysRemaining = (w.EndDate - today).Days,
                VendorName = w.VendorName
            })
            .ToListAsync();

        // 4. Ticket Status Counts
        var allTicketStatuses = await _context.ServiceTickets
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var ticketStatusCounts = new Dictionary<string, int>();
        foreach (var status in Enum.GetValues<TicketStatus>())
        {
            ticketStatusCounts[status.ToString()] = 0;
        }
        foreach (var item in allTicketStatuses)
        {
            ticketStatusCounts[item.Status.ToString()] = item.Count;
        }

        // 5. Total AMC Revenue
        var totalAmcRevenue = await _context.AMCContracts
            .Where(a => a.EndDate >= today)
            .SumAsync(a => a.ContractValue);

        var summary = new Servitore.Shared.Models.DashboardSummary
        {
            TotalCustomers = totalCustomers,
            TotalAssets = totalAssets,
            OpenTickets = openTicketsCount,
            TicketsResolvedToday = ticketsResolvedToday,
            ExpiringWarranties = expiringWarrantiesCount,
            ExpiringAmcContracts = expiringAmcContractsCount,
            RecentNotifications = recentNotifications,
            RecentTickets = recentTickets,
            AmcAlerts = amcAlerts,
            WarrantyAlerts = warrantyAlerts,
            TicketStatusCounts = ticketStatusCounts,
            TotalAmcRevenue = totalAmcRevenue
        };

        return Ok(summary);
    }
}
