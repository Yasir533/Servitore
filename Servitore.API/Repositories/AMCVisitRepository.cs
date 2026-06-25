using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.API.Repositories;

public interface IAMCVisitRepository
{
    Task<AMCVisit?> GetByIdAsync(int id);
    Task<List<AMCVisit>> GetByContractIdAsync(int contractId);
    Task<AMCVisit> AddAsync(AMCVisit visit);
    Task UpdateAsync(AMCVisit visit);
    Task DeleteAsync(int id);
}

public class AMCVisitRepository : IAMCVisitRepository
{
    private readonly AppDbContext _context;

    public AMCVisitRepository(AppDbContext context) => _context = context;

    public Task<AMCVisit?> GetByIdAsync(int id) =>
        _context.AMCVisits
            .Include(v => v.AMCContract)
            .Include(v => v.Engineer)
            .FirstOrDefaultAsync(v => v.Id == id);

    public Task<List<AMCVisit>> GetByContractIdAsync(int contractId) =>
        _context.AMCVisits
            .Include(v => v.Engineer)
            .Where(v => v.AMCContractId == contractId)
            .OrderBy(v => v.ScheduledDate)
            .ToListAsync();

    public async Task<AMCVisit> AddAsync(AMCVisit visit)
    {
        _context.AMCVisits.Add(visit);
        await _context.SaveChangesAsync();
        return visit;
    }

    public async Task UpdateAsync(AMCVisit visit)
    {
        _context.AMCVisits.Update(visit);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var visit = await _context.AMCVisits.FindAsync(id);
        if (visit is null) return;
        _context.AMCVisits.Remove(visit);
        await _context.SaveChangesAsync();
    }
}
