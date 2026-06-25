using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.API.Repositories;

public interface IServiceTicketRepository
{
    Task<ServiceTicket?> GetByIdAsync(int id);
    Task<List<ServiceTicket>> GetAllAsync();
    Task<List<ServiceTicket>> GetOpenTicketsAsync();
    Task<ServiceTicket> AddAsync(ServiceTicket ticket);
    Task UpdateAsync(ServiceTicket ticket);
}

public class ServiceTicketRepository : IServiceTicketRepository
{
    private readonly AppDbContext _context;

    public ServiceTicketRepository(AppDbContext context) => _context = context;

    public Task<ServiceTicket?> GetByIdAsync(int id) =>
        _context.ServiceTickets
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.TicketId == id);

    public Task<List<ServiceTicket>> GetAllAsync() =>
        _context.ServiceTickets
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public Task<List<ServiceTicket>> GetOpenTicketsAsync() =>
        _context.ServiceTickets
            .Include(t => t.Customer)
            .Where(t => t.Status != Servitore.Shared.Enums.TicketStatus.Closed
                     && t.Status != Servitore.Shared.Enums.TicketStatus.Cancelled)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<ServiceTicket> AddAsync(ServiceTicket ticket)
    {
        _context.ServiceTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task UpdateAsync(ServiceTicket ticket)
    {
        _context.ServiceTickets.Update(ticket);
        await _context.SaveChangesAsync();
    }
}
