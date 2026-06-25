using System.ComponentModel.DataAnnotations;

namespace Servitore.API.DTOs;

public class LoginDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
