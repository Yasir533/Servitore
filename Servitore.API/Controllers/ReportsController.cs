using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Reports;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly AppDbContext _context;

    public ReportsController(IExportService exportService, AppDbContext context)
    {
        _exportService = exportService;
        _context = context;
    }

    [HttpGet("tickets/{format}")]
    public async Task<IActionResult> ExportTickets(
        string format,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Servitore.Shared.Enums.ServiceEntryStatus? status = null,
        [FromQuery] Servitore.Shared.Enums.ServiceEntryPriority? priority = null)
    {
        var query = _context.ServiceEntries
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .Include(t => t.AssignedToUser)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedDate >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportServiceEntries(tickets, exportFormat);
        return FileResponse(bytes, exportFormat, $"ServiceTickets_{DateTime.Now:yyyyMMdd}");
    }

    [HttpGet("customers/{format}")]
    public async Task<IActionResult> ExportCustomers(string format)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.CustomerName)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportCustomers(customers, exportFormat);
        return FileResponse(bytes, exportFormat, $"Customers_{DateTime.Now:yyyyMMdd}");
    }

    [HttpGet("assets/{format}")]
    public async Task<IActionResult> ExportAssets(string format)
    {
        var assets = await _context.Assets
            .AsNoTracking()
            .Include(a => a.Customer)
            .OrderBy(a => a.AssetCode)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportAssets(assets, exportFormat);
        return FileResponse(bytes, exportFormat, $"Assets_{DateTime.Now:yyyyMMdd}");
    }

    private static ExportFormat ParseFormat(string format)
    {
        return string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Excel
            : ExportFormat.Pdf;
    }

    private IActionResult FileResponse(byte[] bytes, ExportFormat format, string fileNameBase)
    {
        if (format == ExportFormat.Excel)
        {
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileNameBase}.xlsx");
        }
        else
        {
            return File(bytes, "application/pdf", $"{fileNameBase}.pdf");
        }
    }
}
