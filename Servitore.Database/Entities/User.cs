namespace Servitore.Database.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Role? Role { get; set; }
    public ICollection<ServiceTicket> CreatedTickets { get; set; } = new List<ServiceTicket>();
    public ICollection<ServiceTicket> AssignedTickets { get; set; } = new List<ServiceTicket>();
}
