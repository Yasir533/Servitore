using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new SearchResultDto());
        }

        var term = q.Trim().ToLower();

        // 1. Search Customers
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.CustomerName.ToLower().Contains(term) ||
                        (c.Company != null && c.Company.ToLower().Contains(term)) ||
                        (c.Mobile != null && c.Mobile.Contains(term)) ||
                        (c.Email != null && c.Email.ToLower().Contains(term)))
            .Take(10)
            .Select(c => new SearchItemDto
            {
                Id = c.CustomerId.ToString(),
                Title = c.CustomerName,
                Subtitle = c.Company ?? c.Mobile ?? c.Email ?? string.Empty
            })
            .ToListAsync();

        // 2. Search Products (stored as Assets in DB)
        var products = await _context.Assets
            .AsNoTracking()
            .Where(a => a.ProductName.ToLower().Contains(term) ||
                        a.AssetCode.ToLower().Contains(term) ||
                        (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term)))
            .Take(10)
            .Select(a => new SearchItemDto
            {
                Id = a.AssetId.ToString(),
                Title = a.ProductName,
                Subtitle = $"Code: {a.AssetCode} | Serial: {a.SerialNumber ?? "N/A"}"
            })
            .ToListAsync();

        // 3. Search Service Entries
        var serviceEntries = await _context.ServiceEntries
            .AsNoTracking()
            .Where(se => se.ServiceEntryNumber.ToLower().Contains(term) ||
                         se.ProblemDescription.ToLower().Contains(term))
            .Take(10)
            .Select(se => new SearchItemDto
            {
                Id = se.ServiceEntryId.ToString(),
                Title = se.ServiceEntryNumber,
                Subtitle = se.ProblemDescription.Length > 60 ? se.ProblemDescription.Substring(0, 57) + "..." : se.ProblemDescription
            })
            .ToListAsync();

        // 4. Search Employees (Users)
        var employees = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && 
                        (u.FullName.ToLower().Contains(term) ||
                         u.Username.ToLower().Contains(term) ||
                         u.Email.ToLower().Contains(term)))
            .Take(10)
            .Select(u => new SearchItemDto
            {
                Id = u.Id.ToString(),
                Title = u.FullName,
                Subtitle = $"Username: {u.Username} | Email: {u.Email}"
            })
            .ToListAsync();

        var result = new SearchResultDto
        {
            Customers = customers,
            Products = products,
            ServiceEntries = serviceEntries,
            Employees = employees
        };

        return Ok(result);
    }
}
