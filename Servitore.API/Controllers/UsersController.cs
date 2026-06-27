using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.DTOs;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IActivityLogService _activityLogService;

    public UsersController(IUserService userService, IActivityLogService activityLogService)
    {
        _userService = userService;
        _activityLogService = activityLogService;
    }

    [HttpGet("lookup")]
    [Authorize]
    public async Task<IActionResult> GetLookup()
    {
        var users = await _userService.GetAllAsync();
        var activeUsers = users.Where(u => u.IsActive).Select(u => new Servitore.Shared.Models.UserInfo
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            Role = Enum.TryParse<Servitore.Shared.Enums.UserRole>(u.Role?.RoleName, out var r) ? r : Servitore.Shared.Enums.UserRole.Operator
        }).ToList();
        return Ok(activeUsers);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        var dtos = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            RoleName = u.Role?.RoleName,
            u.IsActive
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();

        var dto = new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            RoleName = user.Role?.RoleName,
            user.IsActive
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserDto dto)
    {
        var created = await _userService.CreateAsync(dto);
        await _activityLogService.LogActivityAsync($"Created User: {created.Username} (ID: {created.Id})", "Users", HttpContext);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UserDto dto)
    {
        dto.Id = id;
        await _userService.UpdateAsync(dto);
        await _activityLogService.LogActivityAsync($"Updated User: {dto.Username} (ID: {id})", "Users", HttpContext);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _userService.DeactivateAsync(id);
        await _activityLogService.LogActivityAsync($"Deactivated User ID: {id}", "Users", HttpContext);
        return NoContent();
    }
}
