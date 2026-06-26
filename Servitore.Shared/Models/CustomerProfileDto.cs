using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class CustomerProfileDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<CustomerProductDto> Products { get; set; } = new();
    public List<CustomerServiceEntryDto> ServiceEntries { get; set; } = new();
}

public class CustomerProductDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CustomerServiceEntryDto
{
    public int ServiceEntryId { get; set; }
    public string ServiceEntryNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? AssignedEngineer { get; set; }
}
