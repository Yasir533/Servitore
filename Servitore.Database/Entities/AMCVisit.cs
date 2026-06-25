using System;
using Servitore.Shared.Enums;

namespace Servitore.Database.Entities;

public class AMCVisit
{
    public int Id { get; set; }
    public int AMCContractId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? VisitDate { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;
    public string? Remarks { get; set; }
    public int? EngineerId { get; set; }

    public AMCContract? AMCContract { get; set; }
    public User? Engineer { get; set; }
}
