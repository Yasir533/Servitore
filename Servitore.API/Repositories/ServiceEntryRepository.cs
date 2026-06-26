using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Servitore.Shared.Enums;

namespace Servitore.API.Repositories;

public interface IServiceEntryRepository
{
    Task<ServiceEntry?> GetByIdAsync(int id);
    Task<List<ServiceEntry>> GetAllAsync();
    Task<List<ServiceEntry>> GetOpenEntriesAsync();
    Task<ServiceEntry> AddAsync(ServiceEntry entry);
    Task UpdateAsync(ServiceEntry entry);
}

public class ServiceEntryRepository : IServiceEntryRepository
{
    private readonly AppDbContext _context;

    public ServiceEntryRepository(AppDbContext context) => _context = context;

    public Task<ServiceEntry?> GetByIdAsync(int id) =>
        _context.ServiceEntries
            .Include(e => e.Customer)
            .Include(e => e.Asset)
            .Include(e => e.History)
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.ServiceEntryId == id);

    public Task<List<ServiceEntry>> GetAllAsync() =>
        _context.ServiceEntries
            .AsNoTracking()
            .Include(e => e.Customer)
            .Include(e => e.Asset)
            .OrderByDescending(e => e.CreatedDate)
            .ToListAsync();

    public Task<List<ServiceEntry>> GetOpenEntriesAsync() =>
        _context.ServiceEntries
            .AsNoTracking()
            .Include(e => e.Customer)
            .Where(e => e.Status != ServiceEntryStatus.Delivered)
            .OrderByDescending(e => e.CreatedDate)
            .ToListAsync();

    public async Task<ServiceEntry> AddAsync(ServiceEntry entry)
    {
        _context.ServiceEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task UpdateAsync(ServiceEntry entry)
    {
        _context.ServiceEntries.Update(entry);
        await _context.SaveChangesAsync();
    }
}
