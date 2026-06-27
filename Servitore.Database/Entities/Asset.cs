using Servitore.Shared.Enums;

namespace Servitore.Database.Entities;

public class Asset : IAuditable, ISoftDeletable
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QRCode { get; set; }
    public string? SerialNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int CustomerId { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public string? VendorName { get; set; }
    public DateTime? PurchaseDate { get; set; }
    
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
    public ICollection<ServiceEntry> ServiceEntries { get; set; } = new List<ServiceEntry>();
    public ICollection<AssetDocument> Documents { get; set; } = new List<AssetDocument>();
}
