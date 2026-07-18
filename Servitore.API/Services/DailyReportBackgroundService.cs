using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Servitore.Shared.Enums;

namespace Servitore.API.Services;

public class DailyReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyReportBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public DailyReportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DailyReportBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[DailyReport] Background service initialized.");

        // Developer-friendly startup trigger for easy verification
        bool runOnStartup = _configuration.GetValue<bool>("DatabaseSettings:RunDailySummaryOnStartup", false) ||
                            _configuration.GetValue<bool>("Smtp:RunDailySummaryOnStartup", false);

        if (runOnStartup)
        {
            _logger.LogInformation("[DailyReport] RunDailySummaryOnStartup is enabled. Triggering daily report immediately.");
            try
            {
                await GenerateAndSendDailyReportAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DailyReport] Error running startup report.");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            // Calculate delay until next midnight
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            _logger.LogInformation("[DailyReport] Next automated report scheduled for {Time} (delaying {Hours:F2} hours).", nextMidnight, delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await GenerateAndSendDailyReportAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DailyReport] Error generating automated daily report.");
            }
        }
    }

    private async Task GenerateAndSendDailyReportAsync()
    {
        _logger.LogInformation("[DailyReport] Starting report generation...");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var receiver = _configuration["Smtp:DailySummaryReceiver"] ?? "admin@servitore.local";
        if (string.IsNullOrWhiteSpace(receiver))
        {
            _logger.LogWarning("[DailyReport] No receiver address configured under Smtp:DailySummaryReceiver. Skipping email dispatch.");
            return;
        }

        var todayLocal = DateTime.Today;
        var tomorrowLocal = todayLocal.AddDays(1);

        // Fetch tickets created today (local time bounds converted to UTC)
        var todayUtcStart = todayLocal.ToUniversalTime();
        var todayUtcEnd = tomorrowLocal.ToUniversalTime();

        var newTickets = await db.ServiceEntries
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .Where(t => t.CreatedDate >= todayUtcStart && t.CreatedDate < todayUtcEnd)
            .ToListAsync();

        var resolvedTickets = await db.ServiceEntries
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.Asset)
            .Where(t => t.ModifiedDate >= todayUtcStart && t.ModifiedDate < todayUtcEnd && t.Status == ServiceEntryStatus.Completed)
            .ToListAsync();

        var totalActivePending = await db.ServiceEntries
            .AsNoTracking()
            .CountAsync(t => t.Status == ServiceEntryStatus.Pending || t.Status == ServiceEntryStatus.InProgress);

        var htmlBody = BuildHtmlReport(todayLocal, newTickets, resolvedTickets, totalActivePending);
        var subject = $"Servitore - Daily Activity Summary ({todayLocal:dd MMM yyyy})";

        await emailService.SendAsync(receiver, subject, htmlBody);
        _logger.LogInformation("[DailyReport] Daily report successfully dispatched to {Receiver}.", receiver);
    }

    private string BuildHtmlReport(DateTime date, System.Collections.Generic.List<ServiceEntry> newTickets, System.Collections.Generic.List<ServiceEntry> resolvedTickets, int totalActivePending)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
        sb.Append("body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f7fafc; color: #2d3748; margin: 0; padding: 20px; }");
        sb.Append(".container { max-width: 600px; background: white; margin: 0 auto; padding: 24px; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); border: 1px solid #e2e8f0; }");
        sb.Append(".header { background-color: #1a365d; color: white; padding: 20px; border-radius: 6px 6px 0 0; text-align: center; margin: -24px -24px 24px -24px; }");
        sb.Append(".header h2 { margin: 0; font-size: 22px; font-weight: 600; }");
        sb.Append(".header p { margin: 4px 0 0 0; font-size: 14px; opacity: 0.85; }");
        sb.Append(".stats-grid { display: table; width: 100%; margin-bottom: 24px; }");
        sb.Append(".stat-box { display: table-cell; width: 33.3%; background: #edf2f7; padding: 16px; border-radius: 6px; text-align: center; border: 4px solid white; }");
        sb.Append(".stat-value { font-size: 24px; font-weight: bold; color: #1a365d; margin-bottom: 4px; }");
        sb.Append(".stat-label { font-size: 12px; text-transform: uppercase; color: #718096; letter-spacing: 0.5px; }");
        sb.Append(".section-title { font-size: 16px; font-weight: bold; color: #1a365d; border-bottom: 2px solid #edf2f7; padding-bottom: 8px; margin-top: 24px; margin-bottom: 12px; }");
        sb.Append("table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }");
        sb.Append("th { background-color: #edf2f7; text-align: left; padding: 8px; font-size: 12px; text-transform: uppercase; color: #4a5568; border-bottom: 1px solid #cbd5e0; }");
        sb.Append("td { padding: 8px; font-size: 13.5px; border-bottom: 1px solid #edf2f7; vertical-align: top; }");
        sb.Append(".badge { display: inline-block; padding: 2px 6px; font-size: 11px; font-weight: bold; border-radius: 4px; }");
        sb.Append(".badge-high { background-color: #fed7d7; color: #c53030; }");
        sb.Append(".badge-normal { background-color: #feebc8; color: #dd6b20; }");
        sb.Append(".badge-low { background-color: #e2e8f0; color: #4a5568; }");
        sb.Append(".footer { font-size: 11.5px; color: #a0aec0; text-align: center; margin-top: 32px; border-top: 1px solid #edf2f7; padding-top: 16px; }");
        sb.Append("</style></head><body>");
        sb.Append("<div class='container'>");
        
        // Header
        sb.Append($"<div class='header'><h2>Servitore Activity Summary</h2><p>{date:dddd, dd MMMM yyyy}</p></div>");

        // Stats Cards
        sb.Append("<div class='stats-grid'>");
        sb.Append($"<div class='stat-box'><div class='stat-value'>{newTickets.Count}</div><div class='stat-label'>New Tickets</div></div>");
        sb.Append($"<div class='stat-box'><div class='stat-value'>{resolvedTickets.Count}</div><div class='stat-label'>Resolved</div></div>");
        sb.Append($"<div class='stat-box'><div class='stat-value'>{totalActivePending}</div><div class='stat-label'>Active Workload</div></div>");
        sb.Append("</div>");

        // New Tickets Section
        sb.Append("<div class='section-title'>New Tickets Created Today</div>");
        if (newTickets.Count > 0)
        {
            sb.Append("<table><thead><tr><th>Ticket #</th><th>Customer</th><th>Product</th><th>Priority</th></tr></thead><tbody>");
            foreach (var t in newTickets)
            {
                var badgeClass = t.Priority == ServiceEntryPriority.High || t.Priority == ServiceEntryPriority.Urgent ? "badge-high" : (t.Priority == ServiceEntryPriority.Normal ? "badge-normal" : "badge-low");
                sb.Append($"<tr><td><b>{t.ServiceEntryNumber}</b></td><td>{t.Customer?.CustomerName ?? "N/A"}</td><td>{t.Asset?.ProductName ?? "N/A"}</td><td><span class='badge {badgeClass}'>{t.Priority}</span></td></tr>");
            }
            sb.Append("</tbody></table>");
        }
        else
        {
            sb.Append("<p style='font-size: 13.5px; italic; color: #718096;'>No new service tickets created today.</p>");
        }

        // Resolved Tickets Section
        sb.Append("<div class='section-title'>Tickets Resolved/Closed Today</div>");
        if (resolvedTickets.Count > 0)
        {
            sb.Append("<table><thead><tr><th>Ticket #</th><th>Customer</th><th>Product</th><th>Solution Summary</th></tr></thead><tbody>");
            foreach (var t in resolvedTickets)
            {
                var solutionSummary = string.IsNullOrWhiteSpace(t.Solution) ? "N/A" : (t.Solution.Length > 50 ? t.Solution.Substring(0, 47) + "..." : t.Solution);
                sb.Append($"<tr><td><b>{t.ServiceEntryNumber}</b></td><td>{t.Customer?.CustomerName ?? "N/A"}</td><td>{t.Asset?.ProductName ?? "N/A"}</td><td>{solutionSummary}</td></tr>");
            }
            sb.Append("</tbody></table>");
        }
        else
        {
            sb.Append("<p style='font-size: 13.5px; italic; color: #718096;'>No tickets resolved today.</p>");
        }

        // Footer
        sb.Append("<div class='footer'>This is an automated operational report generated by Servitore API service.</div>");
        sb.Append("</div></body></html>");

        return sb.ToString();
    }
}
