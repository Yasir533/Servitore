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
    public string BaseUrl { get; }

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

        BaseUrl = baseUrl;
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

    private async Task<T?> ReadContentSafeAsync<T>(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            Helpers.ClientLogger.Log("JSON deserialization failed", ex);
            throw new InvalidOperationException("Failed to process the server response.", ex);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await ReadContentSafeAsync<T>(response.Content);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"GET request failed for endpoint: {endpoint}", ex);
            throw;
        }
    }

    public async Task<byte[]> GetByteArrayAsync(string endpoint)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(endpoint);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"GET byte array request failed for endpoint: {endpoint}", ex);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, body);
            response.EnsureSuccessStatusCode();
            return await ReadContentSafeAsync<TResponse>(response.Content);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"POST request failed for endpoint: {endpoint}", ex);
            throw;
        }
    }

    public async Task PutAsync<TRequest>(string endpoint, TRequest body)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, body);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"PUT request failed for endpoint: {endpoint}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"DELETE request failed for endpoint: {endpoint}", ex);
            throw;
        }
    }

    public async Task<TResponse?> UploadFileAsync<TResponse>(string endpoint, string filePath)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            return await ReadContentSafeAsync<TResponse>(response.Content);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"FileUpload request failed for endpoint: {endpoint}, file: {filePath}", ex);
            throw;
        }
    }
}
