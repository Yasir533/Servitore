using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Servitore.API.DTOs;
using Servitore.API.Repositories;
using Servitore.Shared.Models;

namespace Servitore.API.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginDto dto);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return new LoginResponse { Success = false, Message = "Invalid username or password." };
        }

        var roleName = user.Role?.RoleName ?? "Operator";
        var token = GenerateJwtToken(user.Id, user.Username, roleName);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            Message = "Login successful.",
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = Enum.TryParse<Servitore.Shared.Enums.UserRole>(roleName, out var role)
                    ? role
                    : Servitore.Shared.Enums.UserRole.Operator
            }
        };
    }

    private string GenerateJwtToken(int userId, string username, string roleName)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, roleName)
        };

        var expiry = double.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 480;

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
