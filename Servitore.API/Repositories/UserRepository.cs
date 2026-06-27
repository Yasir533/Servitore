using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> GetAllAsync();
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByIdAsync(int id) =>
        _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string username) =>
        _context.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);

    public Task<List<User>> GetAllAsync() =>
        _context.Users.AsNoTracking().Include(u => u.Role).ToListAsync();

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}
