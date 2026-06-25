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

    public CustomersController(ICustomerService customerService, IActivityLogService activityLogService)
    {
        _customerService = customerService;
        _activityLogService = activityLogService;
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

    [HttpPost]
    public async Task<IActionResult> Create(CustomerDto dto)
    {
        var created = await _customerService.CreateAsync(dto);
        await _activityLogService.LogActivityAsync($"Created Customer: {created.CustomerName} (ID: {created.CustomerId})", "Customers", HttpContext);
        return CreatedAtAction(nameof(GetById), new { id = created.CustomerId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerDto dto)
    {
        dto.CustomerId = id;
        await _customerService.UpdateAsync(dto);
        await _activityLogService.LogActivityAsync($"Updated Customer: {dto.CustomerName} (ID: {id})", "Customers", HttpContext);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        await _activityLogService.LogActivityAsync($"Deleted Customer ID: {id}", "Customers", HttpContext);
        return NoContent();
    }
}
