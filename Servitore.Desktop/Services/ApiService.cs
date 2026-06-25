using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Servitore.Shared.Constants;

namespace Servitore.Desktop.Services;

/// <summary>
/// Thin wrapper around HttpClient for all calls to Servitore.API.
/// Every ViewModel/Service goes through this single class so the base URL,
/// auth header, and JSON settings are configured in exactly one place.
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        var baseUrl = AppConstants.DefaultApiBaseUrl;
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clientSettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ApiBaseUrl", out var prop))
                {
                    var val = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        baseUrl = val;
                    }
                }
            }
            else
            {
                var defaultJson = JsonSerializer.Serialize(new { ApiBaseUrl = AppConstants.DefaultApiBaseUrl }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, defaultJson);
            }
        }
        catch
        {
            // Fallback to default
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void SetAuthToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken() => SetAuthToken(null);

    public Task<T?> GetAsync<T>(string endpoint) =>
        _httpClient.GetFromJsonAsync<T>(endpoint);

    public Task<byte[]> GetByteArrayAsync(string endpoint) =>
        _httpClient.GetByteArrayAsync(endpoint);

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task PutAsync<TRequest>(string endpoint, TRequest body)
    {
        var response = await _httpClient.PutAsJsonAsync(endpoint, body);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TResponse?> UploadFileAsync<TResponse>(string endpoint, string filePath)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }
}
