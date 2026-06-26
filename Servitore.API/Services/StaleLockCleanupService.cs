using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Servitore.API.SignalR;

namespace Servitore.API.Services;

public class StaleLockCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Prune locks older than 4 hours
                RecordLockManager.CleanStaleLocks(TimeSpan.FromHours(4));
            }
            catch (Exception)
            {
                // Suppress background errors
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
