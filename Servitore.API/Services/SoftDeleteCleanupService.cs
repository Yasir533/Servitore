using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Servitore.Database.Context;
using Servitore.API.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Servitore.API.Services;

public class SoftDeleteCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public SoftDeleteCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CollaborationHub>>();

                    // Load retention settings
                    var retentionSetting = await context.Settings.FirstOrDefaultAsync(s => s.Key == "RecentlyDeletedRetentionDays", stoppingToken);
                    int retentionDays = int.TryParse(retentionSetting?.Value, out var rdDays) ? rdDays : 10;
                    var thresholdDate = DateTime.UtcNow.AddDays(-retentionDays);

                    // Delete stale customers
                    var staleCustomers = await context.Customers
                        .IgnoreQueryFilters()
                        .Where(c => c.IsDeleted && c.DeletedDate < thresholdDate)
                        .ToListAsync(stoppingToken);
                    if (staleCustomers.Any())
                    {
                        context.Customers.RemoveRange(staleCustomers);
                    }

                    // Delete stale assets
                    var staleAssets = await context.Assets
                        .IgnoreQueryFilters()
                        .Where(a => a.IsDeleted && a.DeletedDate < thresholdDate)
                        .ToListAsync(stoppingToken);
                    if (staleAssets.Any())
                    {
                        context.Assets.RemoveRange(staleAssets);
                    }

                    // Delete stale service entries
                    var staleServiceEntries = await context.ServiceEntries
                        .IgnoreQueryFilters()
                        .Where(e => e.IsDeleted && e.DeletedDate < thresholdDate)
                        .ToListAsync(stoppingToken);
                    if (staleServiceEntries.Any())
                    {
                        context.ServiceEntries.RemoveRange(staleServiceEntries);
                    }

                    if (staleCustomers.Any() || staleAssets.Any() || staleServiceEntries.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);

                        // Broadcast update to all connected clients
                        await hubContext.Clients.All.SendAsync("DataChanged", new Shared.Models.DataEventModel
                        {
                            EntityType = "SoftDeleteCleanup",
                            Action = "Cleanup",
                            Username = "System"
                        }, stoppingToken);
                    }
                }
            }
            catch (Exception)
            {
                // Suppress background service errors
            }

            // Run cleanup check hourly
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
