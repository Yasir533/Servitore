using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WarrantyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActivityLogService _activityLogService;

    public WarrantyController(AppDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var today = DateTime.UtcNow.Date;
        var results = await _context.Warranties
            .Include(w => w.Asset)
            .ThenInclude(a => a!.Customer)
            .OrderBy(w => w.EndDate)
            .Select(w => new {
                w.WarrantyId,
                w.AssetId,
                CustomerId = w.Asset != null ? w.Asset.CustomerId : 0,
                AssetName = w.Asset != null ? w.Asset.ProductName : string.Empty,
                CustomerName = (w.Asset != null && w.Asset.Customer != null) ? w.Asset.Customer.CustomerName : string.Empty,
                SerialNumber = w.Asset != null ? w.Asset.SerialNumber : string.Empty,
                w.StartDate,
                w.EndDate,
                w.Terms,
                w.VendorName,
                DaysRemaining = (w.EndDate - today).Days,
                WarrantyStatus = w.EndDate < today ? "Expired" : ((w.EndDate - today).Days <= 30 ? "Expiring Soon" : "Active")
            })
            .ToListAsync();
        return Ok(results);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringSoon([FromQuery] int withinDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        var results = await _context.Warranties
            .Include(w => w.Asset)
            .Where(w => w.EndDate <= cutoff && w.EndDate >= DateTime.UtcNow)
            .ToListAsync();
        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Warranty warranty)
    {
        _context.Warranties.Add(warranty);
        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Created Warranty: Asset ID={warranty.AssetId}, End={warranty.EndDate:yyyy-MM-dd} (ID: {warranty.WarrantyId})", "Warranty", HttpContext);
        return CreatedAtAction(nameof(GetExpiringSoon), new { }, warranty);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Warranty warranty)
    {
        var existing = await _context.Warranties.FindAsync(id);
        if (existing is null) return NotFound();

        if (existing.ModifiedDate.HasValue && warranty.ModifiedDate.HasValue &&
            Math.Abs((existing.ModifiedDate.Value - warranty.ModifiedDate.Value).TotalSeconds) > 1.0)
        {
            return Conflict(existing);
        }

        existing.AssetId = warranty.AssetId;
        existing.StartDate = warranty.StartDate;
        existing.EndDate = warranty.EndDate;
        existing.Terms = warranty.Terms;
        existing.VendorName = warranty.VendorName;

        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Updated Warranty ID: {id}", "Warranty", HttpContext);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var warranty = await _context.Warranties.FindAsync(id);
        if (warranty is null) return NotFound();

        _context.Warranties.Remove(warranty);
        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Deleted Warranty ID: {id}", "Warranty", HttpContext);
        return NoContent();
    }
}
