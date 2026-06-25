namespace Servitore.Database.Entities;

public class TicketHistory
{
    public int Id { get; set; }
    public int HistoryId { get; set; }
    public int TicketId { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public ServiceTicket? Ticket { get; set; }
}
