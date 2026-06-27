using Servitore.Shared.Enums;

namespace Servitore.Database.Entities;

public class ServiceEntry : IAuditable, ISoftDeletable
{
    public int ServiceEntryId { get; set; }
    public string ServiceEntryNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int AssetId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public ServiceEntryStatus Status { get; set; } = ServiceEntryStatus.Pending;
    public ServiceEntryPriority Priority { get; set; } = ServiceEntryPriority.Normal;
    public int? AssignedToUserId { get; set; }
    public string? Remarks { get; set; }
    public string? AccessoriesReceived { get; set; }
    public string? Solution { get; set; }
    
    // Upgraded Service Register fields
    public string? ContactPerson { get; set; }
    public string? ContactNumber { get; set; }
    public string? ServiceType { get; set; } = "InHouse";
    public string? CallType { get; set; } = "OOW";
    public string? SubCallType { get; set; }
    public string? AgreementNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public bool IsChargeable { get; set; } = true;
    public string? ComplaintMode { get; set; } = "Phone";
    public bool PendingForDocuments { get; set; }
    public int TomorrowDays { get; set; } = 1;
    public bool IsTomorrow { get; set; }
    public decimal? ApproximateCharges { get; set; }
    public string? CustodyComponentsJson { get; set; }
    
    // Relationship properties
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public User? AssignedToUser { get; set; }

    // Audit fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Soft Delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? DeletedBy { get; set; }

    public Customer? Customer { get; set; }
    public Asset? Asset { get; set; }
    public ICollection<ServiceEntryHistory> History { get; set; } = new List<ServiceEntryHistory>();
    public ICollection<ServiceEntryAttachment> Attachments { get; set; } = new List<ServiceEntryAttachment>();
}
