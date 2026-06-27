using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Servitore.API.DTOs;
using Servitore.API.Repositories;
using Servitore.Database.Context;
using Servitore.Database.Entities;
using Servitore.Shared.Constants;
using Servitore.Shared.Enums;

namespace Servitore.API.Services;

public interface IServiceEntryService
{
    Task<List<ServiceEntry>> GetAllAsync();
    Task<List<ServiceEntry>> GetOpenAsync();
    Task<ServiceEntry?> GetByIdAsync(int id);
    Task<ServiceEntry> CreateAsync(ServiceEntryDto dto, int createdByUserId);
    Task UpdateStatusAsync(int entryId, ServiceEntryStatus newStatus, string updatedBy, string? remarks);
    Task<ServiceEntry> UpdateAsync(int entryId, ServiceEntryDto dto, string updatedBy);
    Task DeleteAsync(int id);
}

public class ServiceEntryService : IServiceEntryService
{
    private readonly IServiceEntryRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly AppDbContext _context;

    public ServiceEntryService(
        IServiceEntryRepository repository,
        INotificationService notificationService,
        IWhatsAppService whatsAppService,
        AppDbContext context)
    {
        _repository = repository;
        _notificationService = notificationService;
        _whatsAppService = whatsAppService;
        _context = context;
    }

    public Task<List<ServiceEntry>> GetAllAsync() => _repository.GetAllAsync();

    public Task<List<ServiceEntry>> GetOpenAsync() => _repository.GetOpenEntriesAsync();

    public Task<ServiceEntry?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<ServiceEntry> CreateAsync(ServiceEntryDto dto, int createdByUserId)
    {
        // 1. Resolve Customer
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Mobile == dto.CustomerMobile);
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Mobile = dto.CustomerMobile,
                Company = dto.CustomerCompany,
                Email = dto.CustomerEmail,
                CreatedBy = createdByUserId.ToString(),
                CreatedDate = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrEmpty(dto.CustomerCompany)) customer.Company = dto.CustomerCompany;
            if (!string.IsNullOrEmpty(dto.CustomerEmail)) customer.Email = dto.CustomerEmail;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        // 2. Resolve Product (Asset)
        Asset? asset = null;
        if (!string.IsNullOrEmpty(dto.ProductSerialNumber))
        {
            asset = await _context.Assets.FirstOrDefaultAsync(a => a.CustomerId == customer.CustomerId && a.SerialNumber == dto.ProductSerialNumber);
        }
        if (asset == null)
        {
            asset = await _context.Assets.FirstOrDefaultAsync(a => a.CustomerId == customer.CustomerId && a.ProductName == dto.ProductName);
        }

        if (asset == null)
        {
            asset = new Asset
            {
                CustomerId = customer.CustomerId,
                ProductName = dto.ProductName,
                Brand = dto.ProductBrand,
                Model = dto.ProductModel,
                SerialNumber = dto.ProductSerialNumber,
                AssetCode = $"PRD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                Status = AssetStatus.Active,
                CreatedBy = createdByUserId.ToString(),
                CreatedDate = DateTime.UtcNow
            };
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrEmpty(dto.ProductBrand)) asset.Brand = dto.ProductBrand;
            if (!string.IsNullOrEmpty(dto.ProductModel)) asset.Model = dto.ProductModel;
            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();
        }

        // 3. Create Service Entry
        var entry = new ServiceEntry
        {
            ServiceEntryNumber = GenerateEntryNumber(),
            CustomerId = customer.CustomerId,
            AssetId = asset.AssetId,
            ProblemDescription = dto.ProblemDescription,
            AccessoriesReceived = dto.AccessoriesReceived,
            Remarks = dto.Remarks,
            Solution = dto.Solution,
            Status = ServiceEntryStatus.Pending,
            Priority = dto.Priority,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedByUserId = createdByUserId,
            CreatedDate = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(entry);

        // Add initial history
        created.History.Add(new ServiceEntryHistory
        {
            ServiceEntryId = created.ServiceEntryId,
            Remarks = "Service Entry created.",
            UpdatedBy = createdByUserId.ToString(),
            UpdatedDate = DateTime.UtcNow
        });
        await _repository.UpdateAsync(created);

        // Real-time fan-out
        await _notificationService.BroadcastAsync(
            NotificationType.ServiceEntryCreated,
            $"Service Entry {created.ServiceEntryNumber} created.",
            createdByUserId.ToString());

        // Send WhatsApp Broadcast
        try
        {
            await _whatsAppService.SendNotificationAsync(
                createdByUserId.ToString(),
                "Created",
                $"Service Entry {created.ServiceEntryNumber} for Customer {customer.CustomerName}"
            );
        }
        catch (Exception)
        {
            // Ignore/Log errors
        }

        return created;
    }

    public async Task UpdateStatusAsync(int entryId, ServiceEntryStatus newStatus, string updatedBy, string? remarks)
    {
        var entry = await _repository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Service Entry not found.");

        entry.Status = newStatus;
        entry.History.Add(new ServiceEntryHistory
        {
            ServiceEntryId = entryId,
            Remarks = remarks ?? $"Status changed to {newStatus}",
            UpdatedBy = updatedBy,
            UpdatedDate = DateTime.UtcNow
        });

        await _repository.UpdateAsync(entry);

        var type = newStatus == ServiceEntryStatus.Completed || newStatus == ServiceEntryStatus.Delivered
            ? NotificationType.ServiceEntryCompleted
            : NotificationType.ServiceEntryUpdated;

        await _notificationService.BroadcastAsync(type, $"Service Entry {entry.ServiceEntryNumber} updated to {newStatus}.", updatedBy);

        // Send WhatsApp Broadcast
        try
        {
            await _whatsAppService.SendNotificationAsync(
                updatedBy,
                "Status Changed",
                $"Service Entry {entry.ServiceEntryNumber} is now {newStatus}"
            );
        }
        catch (Exception)
        {
            // Ignore/Log errors
        }
    }

    public async Task<ServiceEntry> UpdateAsync(int entryId, ServiceEntryDto dto, string updatedBy)
    {
        var entry = await _repository.GetByIdAsync(entryId)
            ?? throw new KeyNotFoundException("Service Entry not found.");

        if (entry.ModifiedDate.HasValue && dto.ModifiedDate.HasValue &&
            Math.Abs((entry.ModifiedDate.Value - dto.ModifiedDate.Value).TotalSeconds) > 1.0)
        {
            throw new DbUpdateConcurrencyException("The service entry record has been modified by another user.");
        }

        // 1. Resolve Customer
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Mobile == dto.CustomerMobile);
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Mobile = dto.CustomerMobile,
                Company = dto.CustomerCompany,
                Email = dto.CustomerEmail,
                CreatedBy = updatedBy,
                CreatedDate = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrEmpty(dto.CustomerCompany)) customer.Company = dto.CustomerCompany;
            if (!string.IsNullOrEmpty(dto.CustomerEmail)) customer.Email = dto.CustomerEmail;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        // 2. Resolve Product (Asset)
        Asset? asset = null;
        if (!string.IsNullOrEmpty(dto.ProductSerialNumber))
        {
            asset = await _context.Assets.FirstOrDefaultAsync(a => a.CustomerId == customer.CustomerId && a.SerialNumber == dto.ProductSerialNumber);
        }
        if (asset == null)
        {
            asset = await _context.Assets.FirstOrDefaultAsync(a => a.CustomerId == customer.CustomerId && a.ProductName == dto.ProductName);
        }

        if (asset == null)
        {
            asset = new Asset
            {
                CustomerId = customer.CustomerId,
                ProductName = dto.ProductName,
                Brand = dto.ProductBrand,
                Model = dto.ProductModel,
                SerialNumber = dto.ProductSerialNumber,
                AssetCode = $"PRD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                Status = AssetStatus.Active,
                CreatedBy = updatedBy,
                CreatedDate = DateTime.UtcNow
            };
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrEmpty(dto.ProductBrand)) asset.Brand = dto.ProductBrand;
            if (!string.IsNullOrEmpty(dto.ProductModel)) asset.Model = dto.ProductModel;
            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();
        }

        var oldStatus = entry.Status;
        var oldEngineer = entry.AssignedToUserId;
        var oldPriority = entry.Priority;

        entry.CustomerId = customer.CustomerId;
        entry.AssetId = asset.AssetId;
        entry.ProblemDescription = dto.ProblemDescription;
        entry.AccessoriesReceived = dto.AccessoriesReceived;
        entry.Solution = dto.Solution;
        entry.Status = dto.Status;
        entry.Priority = dto.Priority;
        entry.AssignedToUserId = dto.AssignedToUserId;
        entry.Remarks = dto.Remarks;
        entry.ModifiedBy = updatedBy;
        entry.ModifiedDate = DateTime.UtcNow;

        var changes = new List<string>();
        if (oldStatus != dto.Status) changes.Add($"Status changed to {dto.Status}");
        if (oldPriority != dto.Priority) changes.Add($"Priority changed to {dto.Priority}");
        if (oldEngineer != dto.AssignedToUserId) changes.Add($"Assigned engineer changed");
        if (!string.IsNullOrWhiteSpace(dto.Remarks)) changes.Add($"Remarks updated");

        var remarks = changes.Count > 0 ? string.Join(", ", changes) : "Service Entry details updated.";

        entry.History.Add(new ServiceEntryHistory
        {
            ServiceEntryId = entryId,
            Remarks = remarks,
            UpdatedBy = updatedBy,
            UpdatedDate = DateTime.UtcNow
        });

        await _repository.UpdateAsync(entry);

        var type = dto.Status == ServiceEntryStatus.Completed || dto.Status == ServiceEntryStatus.Delivered
            ? NotificationType.ServiceEntryCompleted
            : NotificationType.ServiceEntryUpdated;

        await _notificationService.BroadcastAsync(type, $"Service Entry {entry.ServiceEntryNumber} updated: {remarks}.", updatedBy);

        // Send WhatsApp Broadcast
        try
        {
            await _whatsAppService.SendNotificationAsync(
                updatedBy,
                "Updated",
                $"Service Entry {entry.ServiceEntryNumber} status: {entry.Status}"
            );
        }
        catch (Exception)
        {
            // Ignore/Log errors
        }

        return entry;
    }

    public Task DeleteAsync(int id) => _repository.DeleteAsync(id);

    private static string GenerateEntryNumber() =>
        $"SVT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
