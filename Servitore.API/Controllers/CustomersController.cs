using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servitore.API.DTOs;
using Servitore.API.Services;

namespace Servitore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IActivityLogService _activityLogService;
    private readonly IWhatsAppService _whatsAppService;

    public CustomersController(ICustomerService customerService, IActivityLogService activityLogService, IWhatsAppService whatsAppService)
    {
        _customerService = customerService;
        _activityLogService = activityLogService;
        _whatsAppService = whatsAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _customerService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet("{id:int}/profile")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var profile = await _customerService.GetProfileAsync(id);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] string mobile)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(mobile))
        {
            return BadRequest("Name and Mobile are required.");
        }
        var isDuplicate = await _customerService.CheckDuplicateAsync(name, mobile);
        return Ok(new { isDuplicate });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerDto dto)
    {
        var created = await _customerService.CreateAsync(dto);
        await _activityLogService.LogActivityAsync($"Created Customer: {created.CustomerName} (ID: {created.CustomerId})", "Customers", HttpContext);
        
        var username = User.Identity?.Name ?? "system";
        try
        {
            await _whatsAppService.SendNotificationAsync(username, "Created Customer", created.CustomerName);
        }
        catch (Exception) { /* ignore notification errors */ }

        return CreatedAtAction(nameof(GetById), new { id = created.CustomerId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerDto dto)
    {
        dto.CustomerId = id;
        try
        {
            var updated = await _customerService.UpdateAsync(dto);
            await _activityLogService.LogActivityAsync($"Updated Customer: {dto.CustomerName} (ID: {id})", "Customers", HttpContext);
            
            var username = User.Identity?.Name ?? "system";
            try
            {
                await _whatsAppService.SendNotificationAsync(username, "Updated Customer", dto.CustomerName);
            }
            catch (Exception) { /* ignore notification errors */ }

            return Ok(updated);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            var current = await _customerService.GetByIdAsync(id);
            return Conflict(current);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        await _activityLogService.LogActivityAsync($"Deleted Customer ID: {id}", "Customers", HttpContext);
        return NoContent();
    }
}
