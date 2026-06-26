using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Servitore.Database.Context;

namespace Servitore.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUsername()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.Identity?.Name ?? user?.FindFirst(ClaimTypes.Name)?.Value ?? "System";
    }
}
