using System.Net.Http;
using Servitore.Shared.Models;

namespace Servitore.Desktop.Services;

/// <summary>
/// Handles authentication state for the desktop client.
/// Wraps ApiService calls and stores the current user / JWT token.
/// </summary>
public class AuthenticationService
{
    private readonly ApiService _apiService;

    public UserInfo? CurrentUser { get; private set; }
    public string? CurrentToken { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null && !string.IsNullOrEmpty(CurrentToken);

    public AuthenticationService(ApiService apiService) => _apiService = apiService;

    /// <summary>
    /// Posts credentials to api/auth/login. On success stores the token and user info.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        LoginResponse? result;

        try
        {
            result = await _apiService.PostAsync<object, LoginResponse>(
                "api/auth/login",
                new { Username = username, Password = password });
        }
        catch (HttpRequestException ex)
        {
            Helpers.ClientLogger.Log("Authentication service failed to reach API server", ex);
            return new LoginResponse
            {
                Success = false,
                Message = "Unable to connect to the server. Please ensure the server is running."
            };
        }

        if (result is { Success: true })
        {
            CurrentUser = result.User;
            CurrentToken = result.Token;
            _apiService.SetAuthToken(result.Token);
        }

        return result;
    }

    /// <summary>
    /// Clears the session. Call on logout or token expiry.
    /// </summary>
    public void Logout()
    {
        CurrentUser = null;
        CurrentToken = null;
        _apiService.ClearAuthToken();
    }
}
