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
public class AMCController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAMCVisitService _amcVisitService;
    private readonly IActivityLogService _activityLogService;

    public AMCController(AppDbContext context, IAMCVisitService amcVisitService, IActivityLogService activityLogService)
    {
        _context = context;
        _amcVisitService = amcVisitService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var today = DateTime.UtcNow.Date;
        var results = await _context.AMCContracts
            .Include(c => c.Asset)
            .ThenInclude(a => a!.Customer)
            .OrderBy(c => c.EndDate)
            .Select(c => new {
                c.AMCContractId,
                AssetName = c.Asset != null ? c.Asset.ProductName : string.Empty,
                CustomerName = (c.Asset != null && c.Asset.Customer != null) ? c.Asset.Customer.CustomerName : string.Empty,
                c.StartDate,
                c.EndDate,
                c.ContractValue,
                c.VisitsIncluded,
                DaysRemaining = (c.EndDate - today).Days,
                Status = c.EndDate < today ? "Expired" : ((c.EndDate - today).Days <= 30 ? "Expiring Soon" : "Active")
            })
            .ToListAsync();
        return Ok(results);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringSoon([FromQuery] int withinDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        var results = await _context.AMCContracts
            .Include(c => c.Asset)
            .Where(c => c.EndDate <= cutoff && c.EndDate >= DateTime.UtcNow)
            .ToListAsync();
        return Ok(results);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _context.AMCContracts.FindAsync(id);
        if (contract is null) return NotFound();
        _context.AMCContracts.Remove(contract);
        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Deleted AMC Contract ID: {id}", "AMC", HttpContext);
        return NoContent();
    }

    [HttpPost("{id:int}/visits")]
    public async Task<IActionResult> AddVisit(int id, AMCVisit visit)
    {
        var created = await _amcVisitService.AddVisitAsync(id, visit);
        await _activityLogService.LogActivityAsync($"Added Visit to AMC Contract ID: {id} (Visit ID: {created.Id})", "AMC", HttpContext);
        var fullVisit = await _amcVisitService.GetByIdAsync(created.Id);
        if (fullVisit is null) return Ok(created);

        var dto = new Servitore.Shared.Models.AMCVisitDto
        {
            Id = fullVisit.Id,
            AMCContractId = fullVisit.AMCContractId,
            ScheduledDate = fullVisit.ScheduledDate,
            VisitDate = fullVisit.VisitDate,
            Status = fullVisit.Status.ToString(),
            Remarks = fullVisit.Remarks,
            EngineerId = fullVisit.EngineerId,
            EngineerName = fullVisit.Engineer?.FullName
        };
        return Ok(dto);
    }

    [HttpPut("visits/{visitId:int}")]
    public async Task<IActionResult> UpdateVisit(int visitId, AMCVisit visit)
    {
        try
        {
            await _amcVisitService.UpdateVisitAsync(visitId, visit);
            await _activityLogService.LogActivityAsync($"Updated AMC Visit ID: {visitId}", "AMC", HttpContext);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("visits/{visitId:int}")]
    public async Task<IActionResult> DeleteVisit(int visitId)
    {
        var existing = await _amcVisitService.GetByIdAsync(visitId);
        if (existing is null) return NotFound();
        await _amcVisitService.DeleteVisitAsync(visitId);
        await _activityLogService.LogActivityAsync($"Deleted AMC Visit ID: {visitId}", "AMC", HttpContext);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create(AMCContract contract)
    {
        _context.AMCContracts.Add(contract);
        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Created AMC Contract: Start={contract.StartDate:yyyy-MM-dd}, End={contract.EndDate:yyyy-MM-dd} (ID: {contract.AMCContractId})", "AMC", HttpContext);
        return CreatedAtAction(nameof(GetExpiringSoon), new { }, contract);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AMCContract contract)
    {
        var existing = await _context.AMCContracts.FindAsync(id);
        if (existing is null) return NotFound();

        if (existing.ModifiedDate.HasValue && contract.ModifiedDate.HasValue &&
            Math.Abs((existing.ModifiedDate.Value - contract.ModifiedDate.Value).TotalSeconds) > 1.0)
        {
            return Conflict(existing);
        }

        existing.AssetId = contract.AssetId;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.ContractValue = contract.ContractValue;
        existing.VisitsIncluded = contract.VisitsIncluded;
        existing.Status = contract.Status;

        await _context.SaveChangesAsync();
        await _activityLogService.LogActivityAsync($"Updated AMC Contract ID: {id}", "AMC", HttpContext);
        return NoContent();
    }
}
