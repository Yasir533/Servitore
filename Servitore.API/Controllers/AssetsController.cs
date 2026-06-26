using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.Repositories;
using Servitore.Database.Entities;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly IActivityLogService _activityLogService;

    public AssetsController(IAssetRepository assetRepository, IActivityLogService activityLogService)
    {
        _assetRepository = assetRepository;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var assets = await _assetRepository.GetAllAsync();
        var results = assets.Select(a => new {
            a.AssetId,
            a.AssetCode,
            ProductName = a.ProductName,
            a.SerialNumber,
            CustomerName = a.Customer?.CustomerName,
            WarrantyStatus = a.Warranty != null ? (a.Warranty.EndDate >= DateTime.UtcNow ? "Active" : "Expired") : "None",
            Status = a.Status.ToString()
        }).ToList();
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var asset = await _assetRepository.GetByIdAsync(id);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var asset = await _assetRepository.GetByBarcodeAsync(barcode);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpGet("by-customer/{customerId:int}")]
    public async Task<IActionResult> GetByCustomer(int customerId) =>
        Ok(await _assetRepository.GetByCustomerAsync(customerId));

    [HttpPost]
    public async Task<IActionResult> Create(Asset asset)
    {
        var created = await _assetRepository.AddAsync(asset);
        await _activityLogService.LogActivityAsync($"Created Asset: {created.ProductName} ({created.AssetCode})", "Assets", HttpContext);
        return CreatedAtAction(nameof(GetById), new { id = created.AssetId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Asset asset)
    {
        asset.AssetId = id;
        var existing = await _assetRepository.GetByIdAsync(id);
        if (existing is null) return NotFound();

        if (existing.ModifiedDate.HasValue && asset.ModifiedDate.HasValue &&
            Math.Abs((existing.ModifiedDate.Value - asset.ModifiedDate.Value).TotalSeconds) > 1.0)
        {
            return Conflict(existing);
        }

        existing.ProductName = asset.ProductName;
        existing.SerialNumber = asset.SerialNumber;
        existing.CustomerId = asset.CustomerId;
        existing.Status = asset.Status;
        existing.VendorName = asset.VendorName;
        existing.PurchaseDate = asset.PurchaseDate;
        existing.AssetCode = asset.AssetCode;
        existing.Barcode = asset.Barcode;

        await _assetRepository.UpdateAsync(existing);
        await _activityLogService.LogActivityAsync($"Updated Asset: {existing.ProductName} ({existing.AssetCode}, ID: {id})", "Assets", HttpContext);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _assetRepository.DeleteAsync(id);
        await _activityLogService.LogActivityAsync($"Deleted Asset ID: {id}", "Assets", HttpContext);
        return NoContent();
    }

    [HttpGet("{id:int}/profile")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var asset = await _assetRepository.GetProfileAsync(id);
        if (asset is null) return NotFound();

        var dto = new Servitore.Shared.Models.AssetDetailsDto
        {
            AssetId = asset.AssetId,
            AssetCode = asset.AssetCode,
            ProductName = asset.ProductName,
            SerialNumber = asset.SerialNumber,
            CustomerId = asset.CustomerId,
            CustomerName = asset.Customer?.CustomerName ?? string.Empty,
            Status = asset.Status.ToString(),
            VendorName = asset.VendorName,
            PurchaseDate = asset.PurchaseDate,
            Documents = asset.Documents.Select(d => new Servitore.Shared.Models.AssetDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                FilePath = d.FilePath,
                UploadedDate = d.UploadedDate
            }).ToList(),
            Tickets = asset.ServiceTickets.Select(t => new Servitore.Shared.Models.AssetTicketDto
            {
                TicketId = t.TicketId,
                TicketNumber = t.TicketNumber,
                ProblemDescription = t.ProblemDescription,
                Status = t.Status.ToString(),
                CreatedDate = t.CreatedDate
            }).ToList()
        };

        if (asset.Warranty != null)
        {
            dto.Warranty = new Servitore.Shared.Models.AssetWarrantyDto
            {
                WarrantyId = asset.Warranty.WarrantyId,
                StartDate = asset.Warranty.StartDate,
                EndDate = asset.Warranty.EndDate,
                Terms = asset.Warranty.Terms,
                VendorName = asset.Warranty.VendorName,
                Status = asset.Warranty.EndDate >= DateTime.UtcNow ? "Active" : "Expired"
            };
        }

        if (asset.AMCContract != null)
        {
            dto.AMCContract = new Servitore.Shared.Models.AssetAmcDto
            {
                AMCContractId = asset.AMCContract.AMCContractId,
                StartDate = asset.AMCContract.StartDate,
                EndDate = asset.AMCContract.EndDate,
                ContractValue = asset.AMCContract.ContractValue,
                VisitsIncluded = asset.AMCContract.VisitsIncluded,
                Status = asset.AMCContract.EndDate >= DateTime.UtcNow ? "Active" : "Expired",
                Visits = asset.AMCContract.Visits.Select(v => new Servitore.Shared.Models.AMCVisitDto
                {
                    Id = v.Id,
                    AMCContractId = v.AMCContractId,
                    ScheduledDate = v.ScheduledDate,
                    VisitDate = v.VisitDate,
                    Status = v.Status.ToString(),
                    Remarks = v.Remarks,
                    EngineerId = v.EngineerId,
                    EngineerName = v.Engineer?.FullName
                }).ToList()
            };
        }

        return Ok(dto);
    }

    [HttpPost("{id:int}/documents")]
    public async Task<IActionResult> UploadDocument(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) 
            return BadRequest(new { success = false, message = "File is empty." });

        var asset = await _assetRepository.GetByIdAsync(id);
        if (asset == null) 
            return NotFound(new { success = false, message = "Asset not found." });

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var doc = new AssetDocument
        {
            AssetId = id,
            FileName = file.FileName,
            FilePath = filePath,
            UploadedDate = DateTime.UtcNow
        };

        var created = await _assetRepository.AddDocumentAsync(doc);
        await _activityLogService.LogActivityAsync($"Uploaded document '{file.FileName}' for Asset ID: {id}", "Assets", HttpContext);
        return Ok(new Servitore.Shared.Models.AssetDocumentDto
        {
            Id = created.Id,
            FileName = created.FileName,
            FilePath = created.FilePath,
            UploadedDate = created.UploadedDate
        });
    }

    [HttpGet("documents/{docId:int}")]
    public async Task<IActionResult> DownloadDocument(int docId)
    {
        var doc = await _assetRepository.GetDocumentByIdAsync(docId);
        if (doc == null) 
            return NotFound(new { success = false, message = "Document not found." });

        if (!System.IO.File.Exists(doc.FilePath)) 
            return NotFound(new { success = false, message = "Physical file not found on disk." });

        var bytes = await System.IO.File.ReadAllBytesAsync(doc.FilePath);
        return File(bytes, "application/octet-stream", doc.FileName);
    }

    [HttpDelete("documents/{docId:int}")]
    public async Task<IActionResult> DeleteDocument(int docId)
    {
        var doc = await _assetRepository.GetDocumentByIdAsync(docId);
        if (doc == null) 
            return NotFound(new { success = false, message = "Document not found." });

        if (System.IO.File.Exists(doc.FilePath))
        {
            try
            {
                System.IO.File.Delete(doc.FilePath);
            }
            catch {}
        }

        await _assetRepository.DeleteDocumentAsync(docId);
        await _activityLogService.LogActivityAsync($"Deleted document ID: {docId} ('{doc.FileName}') from Asset ID: {doc.AssetId}", "Assets", HttpContext);
        return NoContent();
    }
}
