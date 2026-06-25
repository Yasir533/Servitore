using Microsoft.AspNetCore.Http;
using Servitore.API.Repositories;
using Servitore.Database.Entities;
using Servitore.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Servitore.API.SignalR;

namespace Servitore.API.Services;

public interface IActivityLogService
{
    Task<List<ActivityLogDto>> GetAllLogsAsync();
    Task LogActivityAsync(string action, string module, HttpContext httpContext);
    Task LogActivityAsync(string action, string module, int userId, string userName, HttpContext httpContext);
}

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _repository;
    private readonly IHubContext<CollaborationHub> _hubContext;

    public ActivityLogService(IActivityLogRepository repository, IHubContext<CollaborationHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task<List<ActivityLogDto>> GetAllLogsAsync()
    {
        var logs = await _repository.GetAllAsync();
        return logs.Select(l => new ActivityLogDto
        {
            Id = l.Id,
            LogId = l.LogId,
            Action = l.Action,
            Module = l.Module,
            UserId = l.UserId,
            UserName = l.UserName,
            SystemName = l.SystemName,
            IPAddress = l.IPAddress,
            DateTime = l.DateTime
        }).ToList();
    }

    public async Task LogActivityAsync(string action, string module, HttpContext httpContext)
    {
        var userIdStr = httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        int userId = int.TryParse(userIdStr, out var id) ? id : 0;
        string userName = httpContext.User.Identity?.Name ?? "System";
        await LogActivityAsync(action, module, userId, userName, httpContext);
    }

    public async Task LogActivityAsync(string action, string module, int userId, string userName, HttpContext httpContext)
    {
        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        string userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        var log = new ActivityLog
        {
            LogId = new Random().Next(100000, 999999),
            Action = action,
            Module = module,
            UserId = userId,
            UserName = userName,
            IPAddress = ip,
            SystemName = string.IsNullOrWhiteSpace(userAgent) ? "Desktop Client" : userAgent,
            DateTime = DateTime.UtcNow
        };

        await _repository.AddAsync(log);

        var dto = new ActivityLogDto
        {
            Id = log.Id,
            LogId = log.LogId,
            Action = log.Action,
            Module = log.Module,
            UserId = log.UserId,
            UserName = log.UserName,
            SystemName = log.SystemName,
            IPAddress = log.IPAddress,
            DateTime = log.DateTime
        };
        await _hubContext.Clients.All.SendAsync("ActivityLogged", dto);
    }
}
