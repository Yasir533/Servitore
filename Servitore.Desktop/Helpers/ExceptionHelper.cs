using System;
using System.Net.Http;
using System.Threading.Tasks;
using Servitore.Desktop.Services;

namespace Servitore.Desktop.Helpers;

public static class ExceptionHelper
{
    public static string GetUserFriendlyMessage(Exception ex)
    {
        if (ex is ApiService.ConcurrencyException)
        {
            return "A concurrency conflict occurred. The record was modified by another user. Please reload the data and try again.";
        }

        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.InnerException is System.Net.Sockets.SocketException ||
                httpEx.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                httpEx.Message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase) ||
                httpEx.Message.Contains("refused", StringComparison.OrdinalIgnoreCase))
            {
                return "Server is offline. Please check if the API server is running.";
            }

            if (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "Authentication failed. Please check your credentials or log in again.";
            }

            if (httpEx.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                return "Database is unavailable. Please contact the administrator.";
            }
        }

        if (ex is TaskCanceledException || ex is TimeoutException)
        {
            return "The request timed out. The server might be busy or offline.";
        }

        return "An unexpected error occurred. Please try again later.";
    }
}
