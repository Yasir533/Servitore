using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class CustomerProfileDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<CustomerAssetDto> Assets { get; set; } = new();
    public List<CustomerTicketDto> Tickets { get; set; } = new();
    public List<CustomerAmcDto> AmcContracts { get; set; } = new();
}

public class CustomerAssetDto
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? WarrantyStatus { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
}

public class CustomerTicketDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? AssignedEngineer { get; set; }
}

public class CustomerAmcDto
{
    public int AMCContractId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractValue { get; set; }
    public int VisitsIncluded { get; set; }
    public string Status { get; set; } = string.Empty;
}
