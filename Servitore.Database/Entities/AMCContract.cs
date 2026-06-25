namespace Servitore.Database.Entities;

public class AMCContract
{
    public int AMCContractId { get; set; }
    public int AssetId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractValue { get; set; }
    public int VisitsIncluded { get; set; }
    public string Status { get; set; } = "Active";

    // Audit fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public Asset? Asset { get; set; }
    public ICollection<AMCVisit> Visits { get; set; } = new List<AMCVisit>();
}
