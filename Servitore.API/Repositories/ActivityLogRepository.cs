using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Servitore.API.Repositories;

public interface IActivityLogRepository
{
    Task<List<ActivityLog>> GetAllAsync();
    Task<ActivityLog> AddAsync(ActivityLog log);
}

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly AppDbContext _context;

    public ActivityLogRepository(AppDbContext context) => _context = context;

    public Task<List<ActivityLog>> GetAllAsync() =>
        _context.ActivityLogs
            .OrderByDescending(l => l.DateTime)
            .Take(1000)
            .ToListAsync();

    public async Task<ActivityLog> AddAsync(ActivityLog log)
    {
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }
}
