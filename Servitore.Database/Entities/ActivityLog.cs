namespace Servitore.Database.Entities;

// Most important table — records every meaningful action across every desktop client
// so a full multi-user audit trail (who/what/where/when) is always available.
public class ActivityLog
{
    public int Id { get; set; }
    public int LogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? SystemName { get; set; }
    public string? IPAddress { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
}
