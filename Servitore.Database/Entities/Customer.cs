namespace Servitore.Database.Entities;

public class Customer : IAuditable
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    
    // Audit fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();
}
