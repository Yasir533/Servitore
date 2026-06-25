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
    public async Task<IActionResult> ExportTickets(string format)
    {
        var tickets = await _context.ServiceTickets
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportServiceTickets(tickets, exportFormat);
        return FileResponse(bytes, exportFormat, $"ServiceTickets_{DateTime.Now:yyyyMMdd}");
    }

    [HttpGet("customers/{format}")]
    public async Task<IActionResult> ExportCustomers(string format)
    {
        var customers = await _context.Customers
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
            .Include(a => a.Customer)
            .Include(a => a.Warranty)
            .Include(a => a.AMCContract)
            .OrderBy(a => a.AssetCode)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportAssets(assets, exportFormat);
        return FileResponse(bytes, exportFormat, $"Assets_{DateTime.Now:yyyyMMdd}");
    }

    [HttpGet("warranty/{format}")]
    public async Task<IActionResult> ExportWarranty(string format)
    {
        var warranties = await _context.Warranties
            .Include(w => w.Asset)
            .ThenInclude(a => a.Customer)
            .OrderBy(w => w.EndDate)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportWarrantyReport(warranties, exportFormat);
        return FileResponse(bytes, exportFormat, $"WarrantyReport_{DateTime.Now:yyyyMMdd}");
    }

    [HttpGet("amc/{format}")]
    public async Task<IActionResult> ExportAmc(string format)
    {
        var contracts = await _context.AMCContracts
            .Include(c => c.Asset)
            .ThenInclude(a => a.Customer)
            .OrderBy(c => c.EndDate)
            .ToListAsync();

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.ExportAmcReport(contracts, exportFormat);
        return FileResponse(bytes, exportFormat, $"AmcReport_{DateTime.Now:yyyyMMdd}");
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
