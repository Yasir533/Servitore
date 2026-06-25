namespace Servitore.API.DTOs;

public class UserDto
{
    public int? Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    // Only used when creating/resetting a user; never returned in responses.
    public string? Password { get; set; }
}
