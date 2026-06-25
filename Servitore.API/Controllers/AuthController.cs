using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.DTOs;
using Servitore.API.Services;
using Servitore.Shared.Models;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IActivityLogService _activityLogService;

    public AuthController(IAuthService authService, IActivityLogService activityLogService)
    {
        _authService = authService;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Lightweight ping check for client startup retry connection check.
    /// </summary>
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping()
    {
        return Ok(new { Status = "Healthy" });
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
}
