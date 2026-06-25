using Servitore.API.DTOs;
using Servitore.API.Repositories;
using Servitore.Database.Entities;

namespace Servitore.API.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Servitore.Shared.Models.CustomerProfileDto?> GetProfileAsync(int id);
    Task<Customer> CreateAsync(CustomerDto dto);
    Task UpdateAsync(CustomerDto dto);
    Task DeleteAsync(int id);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public Task<List<Customer>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Customer?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<Servitore.Shared.Models.CustomerProfileDto?> GetProfileAsync(int id)
    {
        var customer = await _repository.GetProfileAsync(id);
        if (customer is null) return null;

        var dto = new Servitore.Shared.Models.CustomerProfileDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            ContactPerson = customer.ContactPerson,
            Mobile = customer.Mobile,
            Email = customer.Email,
            Address = customer.Address,
            CreatedDate = customer.CreatedDate,
            Assets = customer.Assets.Select(a => new Servitore.Shared.Models.CustomerAssetDto
            {
                AssetId = a.AssetId,
                AssetCode = a.AssetCode,
                ProductName = a.ProductName,
                SerialNumber = a.SerialNumber,
                Status = a.Status.ToString(),
                WarrantyStatus = a.Warranty != null ? (a.Warranty.EndDate >= DateTime.UtcNow ? "Active" : "Expired") : "None",
                WarrantyEndDate = a.Warranty?.EndDate
            }).ToList(),
            Tickets = customer.ServiceTickets.Select(t => new Servitore.Shared.Models.CustomerTicketDto
            {
                TicketId = t.TicketId,
                TicketNumber = t.TicketNumber,
                AssetName = t.Asset?.ProductName ?? string.Empty,
                ProblemDescription = t.ProblemDescription,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                CreatedDate = t.CreatedDate,
                AssignedEngineer = t.AssignedToUser?.FullName
            }).ToList()
        };

        foreach (var asset in customer.Assets)
        {
            if (asset.AMCContract != null)
            {
                dto.AmcContracts.Add(new Servitore.Shared.Models.CustomerAmcDto
                {
                    AMCContractId = asset.AMCContract.AMCContractId,
                    AssetName = asset.ProductName,
                    StartDate = asset.AMCContract.StartDate,
                    EndDate = asset.AMCContract.EndDate,
                    ContractValue = asset.AMCContract.ContractValue,
                    VisitsIncluded = asset.AMCContract.VisitsIncluded,
                    Status = asset.AMCContract.EndDate >= DateTime.UtcNow ? "Active" : "Expired"
                });
            }
        }

        return dto;
    }

    public Task<Customer> CreateAsync(CustomerDto dto)
    {
        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            ContactPerson = dto.ContactPerson,
            Mobile = dto.Mobile,
            Email = dto.Email,
            Address = dto.Address
        };

        return _repository.AddAsync(customer);
    }

    public async Task UpdateAsync(CustomerDto dto)
    {
        if (dto.CustomerId is null) throw new ArgumentException("CustomerId is required for update.");

        var customer = await _repository.GetByIdAsync(dto.CustomerId.Value)
            ?? throw new KeyNotFoundException("Customer not found.");

        customer.CustomerName = dto.CustomerName;
        customer.ContactPerson = dto.ContactPerson;
        customer.Mobile = dto.Mobile;
        customer.Email = dto.Email;
        customer.Address = dto.Address;

        await _repository.UpdateAsync(customer);
    }

    public Task DeleteAsync(int id) => _repository.DeleteAsync(id);
}
