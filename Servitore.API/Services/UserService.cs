using Servitore.API.DTOs;
using Servitore.API.Repositories;
using Servitore.Database.Entities;

namespace Servitore.API.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(UserDto dto);
    Task UpdateAsync(UserDto dto);
    Task DeactivateAsync(int id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository) => _repository = repository;

    public Task<List<User>> GetAllAsync() => _repository.GetAllAsync();

    public Task<User?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<User> CreateAsync(UserDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            RoleId = dto.RoleId,
            IsActive = dto.IsActive,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? Guid.NewGuid().ToString("N"))
        };

        return _repository.AddAsync(user);
    }

    public async Task UpdateAsync(UserDto dto)
    {
        if (dto.Id is null) throw new ArgumentException("User Id is required for update.");

        var user = await _repository.GetByIdAsync(dto.Id.Value)
            ?? throw new KeyNotFoundException("User not found.");

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        user.RoleId = dto.RoleId;
        user.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _repository.UpdateAsync(user);
    }

    public async Task DeactivateAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user is null) return;
        user.IsActive = false;
        await _repository.UpdateAsync(user);
    }
}
