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
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        var dtos = customers.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerName = c.CustomerName,
            Company = c.Company,
            Mobile = c.Mobile,
            Email = c.Email,
            Address = c.Address,
            Notes = c.Notes,
            ModifiedDate = c.ModifiedDate
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null) return NotFound();

        var dto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            Company = customer.Company,
            Mobile = customer.Mobile,
            Email = customer.Email,
            Address = customer.Address,
            Notes = customer.Notes,
            ModifiedDate = customer.ModifiedDate
        };
        return Ok(dto);
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

        var createdDto = new CustomerDto
        {
            CustomerId = created.CustomerId,
            CustomerName = created.CustomerName,
            Company = created.Company,
            Mobile = created.Mobile,
            Email = created.Email,
            Address = created.Address,
            Notes = created.Notes,
            ModifiedDate = created.ModifiedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = createdDto.CustomerId }, createdDto);
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

            var updatedDto = new CustomerDto
            {
                CustomerId = updated.CustomerId,
                CustomerName = updated.CustomerName,
                Company = updated.Company,
                Mobile = updated.Mobile,
                Email = updated.Email,
                Address = updated.Address,
                Notes = updated.Notes,
                ModifiedDate = updated.ModifiedDate
            };

            return Ok(updatedDto);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            var current = await _customerService.GetByIdAsync(id);
            if (current is null) return NotFound();

            var currentDto = new CustomerDto
            {
                CustomerId = current.CustomerId,
                CustomerName = current.CustomerName,
                Company = current.Company,
                Mobile = current.Mobile,
                Email = current.Email,
                Address = current.Address,
                Notes = current.Notes,
                ModifiedDate = current.ModifiedDate
            };
            return Conflict(currentDto);
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
