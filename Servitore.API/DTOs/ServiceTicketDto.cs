using Servitore.Shared.Enums;

namespace Servitore.API.DTOs;

public class ServiceTicketDto
{
    public int? TicketId { get; set; }
    public string? TicketNumber { get; set; }
    public int CustomerId { get; set; }
    public int AssetId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public int? AssignedToUserId { get; set; }
    public string? ResolutionNotes { get; set; }
}
