using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.DTOs;
using Servitore.API.Services;
using Servitore.Shared.Models;
using Servitore.Database.Context;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IActivityLogService _activityLogService;
    private readonly AppDbContext _dbContext;

    public AuthController(IAuthService authService, IActivityLogService activityLogService, AppDbContext dbContext)
    {
        _authService = authService;
        _activityLogService = activityLogService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lightweight ping check for client startup retry connection check.
    /// </summary>
    [HttpGet("ping")]
    [AllowAnonymous]
    public async Task<IActionResult> Ping()
    {
        bool dbOnline = false;
        try
        {
            dbOnline = await _dbContext.Database.CanConnectAsync();
        }
        catch
        {
            dbOnline = false;
        }

        return Ok(new
        {
            Status = dbOnline ? "Healthy" : "Degraded",
            Server = "Online",
            Database = dbOnline ? "Online" : "Offline"
        });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT bearer token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(LoginResponse), 401)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
            return Unauthorized(result);

        if (result.User != null)
        {
            await _activityLogService.LogActivityAsync("User logged in successfully", "Auth", result.User.Id, result.User.FullName, HttpContext);
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns the current list of active connected user sessions.
    /// </summary>
    [HttpGet("presence")]
    [Authorize]
    public IActionResult GetPresence()
    {
        return Ok(Servitore.API.SignalR.PresenceManager.GetConnectedUsers());
    }
}
