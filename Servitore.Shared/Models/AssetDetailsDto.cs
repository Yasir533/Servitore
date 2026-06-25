using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class AssetDetailsDto
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public DateTime? PurchaseDate { get; set; }

    public AssetWarrantyDto? Warranty { get; set; }
    public AssetAmcDto? AMCContract { get; set; }

    public List<AssetDocumentDto> Documents { get; set; } = new();
    public List<AssetTicketDto> Tickets { get; set; } = new();
}

public class AssetWarrantyDto
{
    public int WarrantyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Terms { get; set; }
    public string? VendorName { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AssetAmcDto
{
    public int AMCContractId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractValue { get; set; }
    public int VisitsIncluded { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<AMCVisitDto> Visits { get; set; } = new();
}

public class AMCVisitDto
{
    public int Id { get; set; }
    public int AMCContractId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? VisitDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int? EngineerId { get; set; }
    public string? EngineerName { get; set; }
}

public class AssetDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
}

public class AssetTicketDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
