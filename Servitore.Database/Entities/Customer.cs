namespace Servitore.Database.Entities;

public class Customer : IAuditable, ISoftDeletable
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    
    // Audit fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Soft Delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? DeletedBy { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<ServiceEntry> ServiceEntries { get; set; } = new List<ServiceEntry>();
}
