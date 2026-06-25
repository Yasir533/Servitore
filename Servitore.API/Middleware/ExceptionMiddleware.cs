using System.Net;
using System.Text.Json;

namespace Servitore.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);

            context.Response.ContentType = "application/json";
            
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Something went wrong. Please contact the administrator if the problem persists.";

            if (ex is KeyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = ex.Message;
            }
            else if (ex is ArgumentException || ex is InvalidOperationException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = ex.Message;
            }

            context.Response.StatusCode = (int)statusCode;

            var payload = JsonSerializer.Serialize(new
            {
                success = false,
                message = message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
