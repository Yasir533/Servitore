using System;
using Servitore.Shared.Enums;

namespace Servitore.API.DTOs;

public class ServiceEntryDto
{
    public int? ServiceEntryId { get; set; }
    public string? ServiceEntryNumber { get; set; }
    
    // Customer Info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string? CustomerCompany { get; set; }
    public string? CustomerEmail { get; set; }

    // Product Info
    public int AssetId { get; set; } // ProductId
    public string ProductName { get; set; } = string.Empty;
    public string? ProductBrand { get; set; }
    public string? ProductModel { get; set; }
    public string? ProductSerialNumber { get; set; }

    public string ProblemDescription { get; set; } = string.Empty;
    public string? AccessoriesReceived { get; set; }
    public string? Remarks { get; set; } // Internal Remarks
    public string? Solution { get; set; } // Solution / Resolution

    // Upgraded Service Register fields
    public string? ContactPerson { get; set; }
    public string? ContactNumber { get; set; }
    public string? ServiceType { get; set; }
    public string? CallType { get; set; }
    public string? SubCallType { get; set; }
    public string? AgreementNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool IsChargeable { get; set; } = true;
    public string? ComplaintMode { get; set; }
    public bool PendingForDocuments { get; set; }
    public int TomorrowDays { get; set; } = 1;
    public bool IsTomorrow { get; set; }
    public decimal? ApproximateCharges { get; set; }
    public string? CustodyComponentsJson { get; set; }

    public ServiceEntryStatus Status { get; set; } = ServiceEntryStatus.Pending;
    public ServiceEntryPriority Priority { get; set; } = ServiceEntryPriority.Normal;
    public int? AssignedToUserId { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
