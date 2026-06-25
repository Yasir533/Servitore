using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class DashboardSummary
{
    public int TotalCustomers { get; set; }
    public int TotalAssets { get; set; }
    public int OpenTickets { get; set; }
    public int TicketsResolvedToday { get; set; }
    public int ExpiringWarranties { get; set; }
    public int ExpiringAmcContracts { get; set; }
    public List<NotificationModel> RecentNotifications { get; set; } = new();

    // Extended fields
    public List<DashboardTicketDto> RecentTickets { get; set; } = new();
    public List<DashboardAmcAlertDto> AmcAlerts { get; set; } = new();
    public List<DashboardWarrantyAlertDto> WarrantyAlerts { get; set; } = new();
    public Dictionary<string, int> TicketStatusCounts { get; set; } = new();
    public decimal TotalAmcRevenue { get; set; }
}

public class DashboardTicketDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class DashboardAmcAlertDto
{
    public int AMCContractId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public decimal ContractValue { get; set; }
}

public class DashboardWarrantyAlertDto
{
    public int WarrantyId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public string? VendorName { get; set; }
}
