using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class DashboardSummary
{
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int OpenServiceEntries { get; set; }
    public int ServiceEntriesResolvedToday { get; set; }
    public int TodayServiceEntries { get; set; }
    public int OnlineUsers { get; set; }
    public List<NotificationModel> RecentNotifications { get; set; } = new();

    // Extended fields
    public List<DashboardServiceEntryDto> RecentServiceEntries { get; set; } = new();
    public Dictionary<string, int> ServiceEntryStatusCounts { get; set; } = new();
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
    public List<MonthlyResolveMetricDto> MonthlyResolveRates { get; set; } = new();
    public List<ActivityLogDto> RecentActivities { get; set; } = new();

    // Quick View fields
    public int QuickViewServiceCalls { get; set; }
    public int QuickViewPendingCalls { get; set; }
    public int QuickViewDeadlineCalls { get; set; }
    public int QuickViewPriorityCalls { get; set; }
    public int QuickViewRegisteredClosedToday { get; set; }
    public int QuickViewPendingCustomerResponse { get; set; }
    public int QuickViewPendingSpare { get; set; }
    public int QuickViewPendingTechnicalSupport { get; set; }
    public int QuickViewPendingOthers { get; set; }
    public int QuickViewItemNotDelivered { get; set; }
    public int QuickViewUnassignedCalls { get; set; }
    public int QuickViewTransferRequested { get; set; }
    public int QuickViewDeliveryChallan { get; set; }
}

public class MonthlyResolveMetricDto
{
    public string MonthName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double BarHeight { get; set; }
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
