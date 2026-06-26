using Servitore.API.DTOs;
using Servitore.API.Repositories;
using Servitore.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Servitore.API.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Servitore.Shared.Models.CustomerProfileDto?> GetProfileAsync(int id);
    Task<Customer> CreateAsync(CustomerDto dto);
    Task<Customer> UpdateAsync(CustomerDto dto);
    Task DeleteAsync(int id);
    Task<bool> CheckDuplicateAsync(string name, string mobile);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public Task<List<Customer>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Customer?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<bool> CheckDuplicateAsync(string name, string mobile) => _repository.CheckDuplicateAsync(name, mobile);

    public async Task<Servitore.Shared.Models.CustomerProfileDto?> GetProfileAsync(int id)
    {
        var customer = await _repository.GetProfileAsync(id);
        if (customer is null) return null;

        var dto = new Servitore.Shared.Models.CustomerProfileDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            Company = customer.Company,
            Mobile = customer.Mobile,
            Email = customer.Email,
            Address = customer.Address,
            CreatedDate = customer.CreatedDate,
            Products = customer.Assets.Select(a => new Servitore.Shared.Models.CustomerProductDto
            {
                ProductId = a.AssetId,
                ProductCode = a.AssetCode,
                ProductName = a.ProductName,
                SerialNumber = a.SerialNumber,
                Status = a.Status.ToString()
            }).ToList(),
            ServiceEntries = customer.ServiceEntries.Select(t => new Servitore.Shared.Models.CustomerServiceEntryDto
            {
                ServiceEntryId = t.ServiceEntryId,
                ServiceEntryNumber = t.ServiceEntryNumber,
                ProductName = t.Asset?.ProductName ?? string.Empty,
                ProblemDescription = t.ProblemDescription,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                CreatedDate = t.CreatedDate,
                AssignedEngineer = t.AssignedToUser?.FullName
            }).ToList()
        };

        return dto;
    }

    public Task<Customer> CreateAsync(CustomerDto dto)
    {
        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            Company = dto.Company,
            Mobile = dto.Mobile,
            Email = dto.Email,
            Address = dto.Address,
            Notes = dto.Notes
        };

        return _repository.AddAsync(customer);
    }

    public async Task<Customer> UpdateAsync(CustomerDto dto)
    {
        if (dto.CustomerId is null) throw new ArgumentException("CustomerId is required for update.");

        var customer = await _repository.GetByIdAsync(dto.CustomerId.Value)
            ?? throw new KeyNotFoundException("Customer not found.");

        if (customer.ModifiedDate.HasValue && dto.ModifiedDate.HasValue &&
            Math.Abs((customer.ModifiedDate.Value - dto.ModifiedDate.Value).TotalSeconds) > 1.0)
        {
            throw new DbUpdateConcurrencyException("The customer record has been modified by another user.");
        }

        customer.CustomerName = dto.CustomerName;
        customer.Company = dto.Company;
        customer.Mobile = dto.Mobile;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Notes = dto.Notes;

        await _repository.UpdateAsync(customer);
        return customer;
    }

    public Task DeleteAsync(int id) => _repository.DeleteAsync(id);
}
