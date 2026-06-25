using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class TicketDetailsDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? SlaDueDate { get; set; }
    public bool SlaBreached { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public List<TicketHistoryDto> History { get; set; } = new();
}

public class TicketHistoryDto
{
    public int Id { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
}
