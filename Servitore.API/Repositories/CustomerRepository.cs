using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.API.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetProfileAsync(int id);
    Task<List<Customer>> GetAllAsync();
    Task<Customer> AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<bool> CheckDuplicateAsync(string name, string mobile);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) => _context = context;

    public Task<Customer?> GetByIdAsync(int id) =>
        _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);

    public Task<Customer?> GetProfileAsync(int id) =>
        _context.Customers
            .AsNoTracking()
            .Include(c => c.Assets)
            .Include(c => c.ServiceEntries)
                .ThenInclude(t => t.Asset)
            .Include(c => c.ServiceEntries)
                .ThenInclude(t => t.AssignedToUser)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

    public Task<List<Customer>> GetAllAsync() =>
        _context.Customers.AsNoTracking().OrderBy(c => c.CustomerName).ToListAsync();

    public async Task<Customer> AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null) return;
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }

    public Task<bool> CheckDuplicateAsync(string name, string mobile)
    {
        var cleanedName = name.Trim().ToLower();
        var cleanedMobile = mobile.Trim();
        return _context.Customers.AnyAsync(c => 
            c.CustomerName.Trim().ToLower() == cleanedName && 
            c.Mobile != null && c.Mobile.Trim() == cleanedMobile);
    }
}
