namespace Servitore.Database.Entities;

public class Warranty
{
    public int WarrantyId { get; set; }
    public int AssetId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Terms { get; set; }
    public string? VendorName { get; set; }
    
    // Audit fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public Asset? Asset { get; set; }
}
