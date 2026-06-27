using System;
using System.Collections.Generic;
using Servitore.Shared.Enums;

namespace Servitore.Shared.Models;

public class ServiceEntryDetailsDto
{
    public int ServiceEntryId { get; set; }
    public string ServiceEntryNumber { get; set; } = string.Empty;
    
    // Customer Info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string? CustomerCompany { get; set; }
    public string? CustomerEmail { get; set; }

    // Product Info
    public int ProductId { get; set; }
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
    public bool IsChargeable { get; set; }
    public string? ComplaintMode { get; set; }
    public bool PendingForDocuments { get; set; }
    public int TomorrowDays { get; set; }
    public bool IsTomorrow { get; set; }
    public decimal? ApproximateCharges { get; set; }
    public string? CustodyComponentsJson { get; set; }

    public ServiceEntryPriority Priority { get; set; } = ServiceEntryPriority.Normal;
    public ServiceEntryStatus Status { get; set; } = ServiceEntryStatus.Pending;
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime? SlaDueDate { get; set; }
    public bool SlaBreached { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public List<ServiceEntryHistoryDto> History { get; set; } = new();
    public List<ServiceEntryAttachmentDto> Attachments { get; set; } = new();
}

public class ServiceEntryAttachmentDto
{
    public int Id { get; set; }
    public int ServiceEntryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string AttachmentType { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
}

public class ServiceEntryHistoryDto
{
    public int Id { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
}
