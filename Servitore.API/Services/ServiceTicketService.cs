using Servitore.API.DTOs;
using Servitore.API.Repositories;
using Servitore.Database.Entities;
using Servitore.Shared.Constants;
using Servitore.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Servitore.API.Services;

public interface IServiceTicketService
{
    Task<List<ServiceTicket>> GetAllAsync();
    Task<List<ServiceTicket>> GetOpenAsync();
    Task<ServiceTicket?> GetByIdAsync(int id);
    Task<ServiceTicket> CreateAsync(ServiceTicketDto dto, int createdByUserId);
    Task UpdateStatusAsync(int ticketId, TicketStatus newStatus, string updatedBy, string? remarks);
    Task<ServiceTicket> UpdateAsync(int ticketId, ServiceTicketDto dto, string updatedBy);
}

public class ServiceTicketService : IServiceTicketService
{
    private readonly IServiceTicketRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IWhatsAppService _whatsAppService;

    public ServiceTicketService(
        IServiceTicketRepository repository,
        INotificationService notificationService,
        IWhatsAppService whatsAppService)
    {
        _repository = repository;
        _notificationService = notificationService;
        _whatsAppService = whatsAppService;
    }

    public Task<List<ServiceTicket>> GetAllAsync() => _repository.GetAllAsync();

    public Task<List<ServiceTicket>> GetOpenAsync() => _repository.GetOpenTicketsAsync();

    public Task<ServiceTicket?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<ServiceTicket> CreateAsync(ServiceTicketDto dto, int createdByUserId)
    {
        var slaHours = dto.Priority switch
        {
            TicketPriority.Critical => 4,
            TicketPriority.High => 24,
            TicketPriority.Medium => 72, // 3 days
            TicketPriority.Low => 168,  // 7 days
            _ => 72
        };

        var ticket = new ServiceTicket
        {
            TicketNumber = GenerateTicketNumber(),
            CustomerId = dto.CustomerId,
            AssetId = dto.AssetId,
            ProblemDescription = dto.ProblemDescription,
            Status = TicketStatus.Open,
            Priority = dto.Priority,
            AssignedToUserId = dto.AssignedToUserId,
            SlaDueDate = DateTime.UtcNow.AddHours(slaHours),
            SlaBreached = false,
            CreatedByUserId = createdByUserId,
            CreatedDate = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(ticket);

        // Add initial history
        created.History.Add(new TicketHistory
        {
            TicketId = created.TicketId,
            Remarks = "Ticket created.",
            UpdatedBy = createdByUserId.ToString(),
            UpdatedDate = DateTime.UtcNow
        });
        await _repository.UpdateAsync(created);

        // Real-time fan-out
        await _notificationService.BroadcastAsync(
            NotificationType.TicketCreated,
            $"Ticket {created.TicketNumber} created.",
            createdByUserId.ToString());

        await _whatsAppService.SendTicketCreatedAsync(created);

        return created;
    }

    public async Task UpdateStatusAsync(int ticketId, TicketStatus newStatus, string updatedBy, string? remarks)
    {
        var ticket = await _repository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException("Ticket not found.");

        ticket.Status = newStatus;
        ticket.History.Add(new TicketHistory
        {
            TicketId = ticketId,
            Remarks = remarks ?? $"Status changed to {newStatus}",
            UpdatedBy = updatedBy,
            UpdatedDate = DateTime.UtcNow
        });

        await _repository.UpdateAsync(ticket);

        var type = newStatus == TicketStatus.Resolved || newStatus == TicketStatus.Closed
            ? NotificationType.TicketCompleted
            : NotificationType.TicketUpdated;

        await _notificationService.BroadcastAsync(type, $"Ticket {ticket.TicketNumber} updated to {newStatus}.", updatedBy);

        if (type == NotificationType.TicketCompleted)
        {
            await _whatsAppService.SendTicketCompletedAsync(ticket);
        }
        else
        {
            await _whatsAppService.SendTicketUpdatedAsync(ticket);
        }
    }

    public async Task<ServiceTicket> UpdateAsync(int ticketId, ServiceTicketDto dto, string updatedBy)
    {
        var ticket = await _repository.GetByIdAsync(ticketId)
            ?? throw new KeyNotFoundException("Ticket not found.");

        if (ticket.ModifiedDate.HasValue && dto.ModifiedDate.HasValue &&
            Math.Abs((ticket.ModifiedDate.Value - dto.ModifiedDate.Value).TotalSeconds) > 1.0)
        {
            throw new DbUpdateConcurrencyException("The ticket record has been modified by another user.");
        }

        var oldStatus = ticket.Status;
        var oldEngineer = ticket.AssignedToUserId;
        var oldPriority = ticket.Priority;

        ticket.Status = dto.Status;
        ticket.Priority = dto.Priority;
        ticket.AssignedToUserId = dto.AssignedToUserId;
        ticket.ResolutionNotes = dto.ResolutionNotes;
        ticket.ModifiedBy = updatedBy;
        ticket.ModifiedDate = DateTime.UtcNow;

        if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
        {
            if (ticket.SlaDueDate.HasValue && DateTime.UtcNow > ticket.SlaDueDate.Value)
            {
                ticket.SlaBreached = true;
            }
        }

        var changes = new List<string>();
        if (oldStatus != dto.Status) changes.Add($"Status changed to {dto.Status}");
        if (oldPriority != dto.Priority) changes.Add($"Priority changed to {dto.Priority}");
        if (oldEngineer != dto.AssignedToUserId) changes.Add($"Assigned engineer changed");
        if (!string.IsNullOrWhiteSpace(dto.ResolutionNotes)) changes.Add($"Resolution notes updated");

        var remarks = changes.Count > 0 ? string.Join(", ", changes) : "Ticket details updated.";

        ticket.History.Add(new TicketHistory
        {
            TicketId = ticketId,
            Remarks = remarks,
            UpdatedBy = updatedBy,
            UpdatedDate = DateTime.UtcNow
        });

        await _repository.UpdateAsync(ticket);

        var type = dto.Status == TicketStatus.Resolved || dto.Status == TicketStatus.Closed
            ? NotificationType.TicketCompleted
            : NotificationType.TicketUpdated;

        await _notificationService.BroadcastAsync(type, $"Ticket {ticket.TicketNumber} updated: {remarks}.", updatedBy);
        return ticket;
    }

    private static string GenerateTicketNumber() =>
        $"{AppConstants.TicketNumberPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
