using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class DashboardSummary
{
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int OpenServiceEntries { get; set; }
    public int ServiceEntriesResolvedToday { get; set; }
    public List<NotificationModel> RecentNotifications { get; set; } = new();

    // Extended fields
    public List<DashboardServiceEntryDto> RecentServiceEntries { get; set; } = new();
    public Dictionary<string, int> ServiceEntryStatusCounts { get; set; } = new();
    public List<ActivityLogDto> RecentActivities { get; set; } = new();
}

public class DashboardServiceEntryDto
{
    public int ServiceEntryId { get; set; }
    public string ServiceEntryNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
