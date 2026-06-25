using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.DTOs;
using Servitore.API.Services;
using Servitore.Shared.Enums;
using Servitore.Shared.Models;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceTicketsController : ControllerBase
{
    private readonly IServiceTicketService _ticketService;
    private readonly IActivityLogService _activityLogService;

    public ServiceTicketsController(IServiceTicketService ticketService, IActivityLogService activityLogService)
    {
        _ticketService = ticketService;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _ticketService.GetAllAsync());

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen() => Ok(await _ticketService.GetOpenAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket is null) return NotFound();

        var dto = new TicketDetailsDto
        {
            TicketId = ticket.TicketId,
            TicketNumber = ticket.TicketNumber,
            CustomerId = ticket.CustomerId,
            CustomerName = ticket.Customer?.CustomerName ?? string.Empty,
            AssetId = ticket.AssetId,
            AssetName = ticket.Asset?.ProductName ?? string.Empty,
            ProblemDescription = ticket.ProblemDescription,
            Priority = ticket.Priority.ToString(),
            Status = ticket.Status.ToString(),
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToUserName = ticket.AssignedToUser?.FullName,
            ResolutionNotes = ticket.ResolutionNotes,
            SlaDueDate = ticket.SlaDueDate,
            SlaBreached = ticket.SlaBreached,
            CreatedBy = ticket.CreatedByUserId,
            CreatedByUserName = ticket.CreatedByUser?.FullName ?? string.Empty,
            CreatedDate = ticket.CreatedDate,
            History = ticket.History.OrderByDescending(h => h.UpdatedDate).Select(h => new TicketHistoryDto
            {
                Id = h.HistoryId,
                Remarks = h.Remarks,
                UpdatedBy = h.UpdatedBy,
                UpdatedDate = h.UpdatedDate
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceTicketDto dto)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            userId = 0;
        }
        var created = await _ticketService.CreateAsync(dto, userId);
        await _activityLogService.LogActivityAsync($"Created Service Ticket: {created.TicketNumber} (ID: {created.TicketId})", "Tickets", HttpContext);
        return CreatedAtAction(nameof(GetById), new { id = created.TicketId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ServiceTicketDto dto)
    {
        var updatedBy = User.Identity?.Name ?? "system";
        await _ticketService.UpdateAsync(id, dto, updatedBy);
        await _activityLogService.LogActivityAsync($"Updated Service Ticket: {dto.TicketNumber} (ID: {id})", "Tickets", HttpContext);
        return NoContent();
    }

    [HttpPut("{id:int}/close")]
    public async Task<IActionResult> CloseTicket(int id)
    {
        var updatedBy = User.Identity?.Name ?? "system";
        await _ticketService.UpdateStatusAsync(id, TicketStatus.Closed, updatedBy, "Ticket closed by operator.");
        await _activityLogService.LogActivityAsync($"Closed Service Ticket ID: {id}", "Tickets", HttpContext);
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var updatedBy = User.Identity?.Name ?? "system";
        await _ticketService.UpdateStatusAsync(id, request.Status, updatedBy, request.Remarks);
        await _activityLogService.LogActivityAsync($"Updated status of Ticket ID: {id} to {request.Status}", "Tickets", HttpContext);
        return NoContent();
    }

    public class UpdateStatusRequest
    {
        public TicketStatus Status { get; set; }
        public string? Remarks { get; set; }
    }
}
