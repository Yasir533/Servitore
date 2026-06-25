namespace Servitore.API.Middleware;

// Lightweight placeholder for any additional per-request JWT/claims handling
// beyond what the built-in JwtBearer authentication handler already does
// (e.g. attaching the resolved user to HttpContext.Items for downstream use).
public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            context.Items["UserId"] = userId;
        }

        await _next(context);
    }
}
