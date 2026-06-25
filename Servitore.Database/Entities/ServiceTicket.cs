using Servitore.Shared.Enums;

namespace Servitore.Database.Entities;

public class ServiceTicket : IAuditable
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int AssetId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public int? AssignedToUserId { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? SlaDueDate { get; set; }
    public bool SlaBreached { get; set; }
    
    // Relationship properties
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public User? AssignedToUser { get; set; }

    // Audit fields
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public Customer? Customer { get; set; }
    public Asset? Asset { get; set; }
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}
