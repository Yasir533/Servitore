using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Servitore.API.DTOs;
using Servitore.API.Services;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceEntriesController : ControllerBase
{
    private readonly IServiceEntryService _entryService;
    private readonly IActivityLogService _activityLogService;
    private readonly AppDbContext _context;

    public ServiceEntriesController(IServiceEntryService entryService, IActivityLogService activityLogService, AppDbContext context)
    {
        _entryService = entryService;
        _activityLogService = activityLogService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _entryService.GetAllAsync());

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen() => Ok(await _entryService.GetOpenAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entry = await _entryService.GetByIdAsync(id);
        if (entry is null) return NotFound();

        var dto = new ServiceEntryDetailsDto
        {
            ServiceEntryId = entry.ServiceEntryId,
            ServiceEntryNumber = entry.ServiceEntryNumber,
            CustomerId = entry.CustomerId,
            CustomerName = entry.Customer?.CustomerName ?? string.Empty,
            CustomerMobile = entry.Customer?.Mobile ?? string.Empty,
            CustomerCompany = entry.Customer?.Company,
            CustomerEmail = entry.Customer?.Email,
            ProductId = entry.AssetId,
            ProductName = entry.Asset?.ProductName ?? string.Empty,
            ProductBrand = entry.Asset?.Brand,
            ProductModel = entry.Asset?.Model,
            ProductSerialNumber = entry.Asset?.SerialNumber,
            ProblemDescription = entry.ProblemDescription,
            AccessoriesReceived = entry.AccessoriesReceived,
            Remarks = entry.Remarks,
            Solution = entry.Solution,
            Priority = entry.Priority,
            Status = entry.Status,
            AssignedToUserId = entry.AssignedToUserId,
            AssignedToUserName = entry.AssignedToUser?.FullName,
            CreatedBy = entry.CreatedByUserId,
            CreatedByUserName = entry.CreatedByUser?.FullName ?? string.Empty,
            CreatedDate = entry.CreatedDate,
            History = entry.History.OrderByDescending(h => h.UpdatedDate).Select(h => new ServiceEntryHistoryDto
            {
                Id = h.HistoryId,
                Remarks = h.Remarks,
                UpdatedBy = h.UpdatedBy,
                UpdatedDate = h.UpdatedDate
            }).ToList(),
            Attachments = entry.Attachments.Select(a => new ServiceEntryAttachmentDto
            {
                Id = a.Id,
                ServiceEntryId = a.ServiceEntryId,
                FileName = a.FileName,
                FilePath = a.FilePath,
                AttachmentType = a.AttachmentType,
                UploadedDate = a.UploadedDate
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceEntryDto dto)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            userId = 0;
        }
        var created = await _entryService.CreateAsync(dto, userId);
        await _activityLogService.LogActivityAsync($"Created Service Entry: {created.ServiceEntryNumber} (ID: {created.ServiceEntryId})", "ServiceEntries", HttpContext);
        return CreatedAtAction(nameof(GetById), new { id = created.ServiceEntryId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ServiceEntryDto dto)
    {
        var updatedBy = User.Identity?.Name ?? "system";
        try
        {
            var updated = await _entryService.UpdateAsync(id, dto, updatedBy);
            await _activityLogService.LogActivityAsync($"Updated Service Entry: {dto.ServiceEntryNumber} (ID: {id})", "ServiceEntries", HttpContext);
            return Ok(updated);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            var current = await _entryService.GetByIdAsync(id);
            return Conflict(current);
        }
    }

    [HttpPut("{id:int}/close")]
    public async Task<IActionResult> CloseEntry(int id)
    {
        var updatedBy = User.Identity?.Name ?? "system";
        await _entryService.UpdateStatusAsync(id, ServiceEntryStatus.Delivered, updatedBy, "Service entry delivered to customer.");
        await _activityLogService.LogActivityAsync($"Delivered Service Entry ID: {id}", "ServiceEntries", HttpContext);
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var updatedBy = User.Identity?.Name ?? "system";
        await _entryService.UpdateStatusAsync(id, request.Status, updatedBy, request.Remarks);
        await _activityLogService.LogActivityAsync($"Updated status of Service Entry ID: {id} to {request.Status}", "ServiceEntries", HttpContext);
        return NoContent();
    }

    [HttpPost("{id:int}/attachments")]
    public async Task<IActionResult> UploadAttachment(int id, IFormFile file, [FromForm] string? attachmentType)
    {
        if (file == null || file.Length == 0) return BadRequest("File is empty.");

        var entry = await _entryService.GetByIdAsync(id);
        if (entry == null) return NotFound("Service Entry not found.");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "attachments");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new ServiceEntryAttachment
        {
            ServiceEntryId = id,
            FileName = file.FileName,
            FilePath = filePath,
            AttachmentType = attachmentType ?? "Other",
            UploadedDate = DateTime.UtcNow
        };

        _context.ServiceEntryAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        var dto = new ServiceEntryAttachmentDto
        {
            Id = attachment.Id,
            ServiceEntryId = attachment.ServiceEntryId,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            AttachmentType = attachment.AttachmentType,
            UploadedDate = attachment.UploadedDate
        };

        return Ok(dto);
    }

    [HttpGet("attachments/{attachmentId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var attachment = await _context.ServiceEntryAttachments.FindAsync(attachmentId);
        if (attachment == null) return NotFound("Attachment not found.");

        if (!System.IO.File.Exists(attachment.FilePath))
        {
            return NotFound("File not found on disk.");
        }

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(attachment.FileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);
        return File(bytes, contentType, attachment.FileName);
    }

    [HttpDelete("attachments/{attachmentId:int}")]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        var attachment = await _context.ServiceEntryAttachments.FindAsync(attachmentId);
        if (attachment == null) return NotFound("Attachment not found.");

        try
        {
            if (System.IO.File.Exists(attachment.FilePath))
            {
                System.IO.File.Delete(attachment.FilePath);
            }
        }
        catch
        {
            // Ignore disk file deletion errors
        }

        _context.ServiceEntryAttachments.Remove(attachment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    public class UpdateStatusRequest
    {
        public ServiceEntryStatus Status { get; set; }
        public string? Remarks { get; set; }
    }
}
