using System;
using System.Collections.Generic;

namespace Servitore.Shared.Models;

public class ProductDetailsDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public DateTime? PurchaseDate { get; set; }

    public List<ProductDocumentDto> Documents { get; set; } = new();
    public List<ProductServiceEntryDto> ServiceEntries { get; set; } = new();
}

public class ProductDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
}

public class ProductServiceEntryDto
{
    public int ServiceEntryId { get; set; }
    public string ServiceEntryNumber { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
