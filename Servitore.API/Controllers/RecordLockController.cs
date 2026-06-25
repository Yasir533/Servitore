using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Servitore.API.SignalR;
using Servitore.Shared.Models;
using Servitore.Shared.Enums;

namespace Servitore.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecordLockController : ControllerBase
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public RecordLockController(IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("acquire")]
    public async Task<IActionResult> AcquireLock([FromBody] RecordLockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RecordKey) || string.IsNullOrWhiteSpace(request.ConnectionId))
        {
            return BadRequest("RecordKey and ConnectionId are required.");
        }

        var username = User.Identity?.Name ?? "Unknown";
        var existingLock = RecordLockManager.AcquireLock(request.RecordKey, username, request.ConnectionId);

        if (existingLock == null)
        {
            return BadRequest("Could not manage lock.");
        }

        // Lock is either new or already held by the same user
        if (existingLock.Username == username && existingLock.ConnectionId == request.ConnectionId)
        {
            await _hubContext.Clients.All.SendAsync("LocksUpdated");
            return Ok(new { Success = true, Lock = existingLock });
        }

        // Lock belongs to someone else
        return Ok(new { Success = false, Lock = existingLock });
    }

    [HttpPost("release")]
    public async Task<IActionResult> ReleaseLock([FromBody] RecordLockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RecordKey))
        {
            return BadRequest("RecordKey is required.");
        }

        var username = User.Identity?.Name ?? "Unknown";
        var released = RecordLockManager.ReleaseLock(request.RecordKey, username);

        if (released)
        {
            await _hubContext.Clients.All.SendAsync("LocksUpdated");
            return Ok(new { Success = true });
        }

        return Ok(new { Success = false, Message = "Lock is held by another user or not found." });
    }

    [HttpPost("takeover")]
    public async Task<IActionResult> TakeOverLock([FromBody] RecordLockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RecordKey) || string.IsNullOrWhiteSpace(request.ConnectionId))
        {
            return BadRequest("RecordKey and ConnectionId are required.");
        }

        // Takeover is allowed for Admins and Managers
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        var username = User.Identity?.Name ?? "Unknown";

        // Get the old lock to notify them
        var oldLock = RecordLockManager.GetLock(request.RecordKey);
        
        RecordLockManager.ForceReleaseLock(request.RecordKey);
        var newLock = RecordLockManager.AcquireLock(request.RecordKey, username, request.ConnectionId);

        if (newLock != null)
        {
            await _hubContext.Clients.All.SendAsync("LocksUpdated");
            
            // Notify the previous lock owner if they are still connected
            if (oldLock != null && !string.IsNullOrWhiteSpace(oldLock.ConnectionId))
            {
                await _hubContext.Clients.Client(oldLock.ConnectionId).SendAsync("LockTakenOver", new { RecordKey = request.RecordKey, NewOwner = username });
            }

            return Ok(new { Success = true, Lock = newLock });
        }

        return BadRequest("Failed to acquire takeover lock.");
    }
}

public class RecordLockRequest
{
    public string RecordKey { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
}
