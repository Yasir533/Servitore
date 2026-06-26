namespace Servitore.API.DTOs;

public class CustomerDto
{
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
