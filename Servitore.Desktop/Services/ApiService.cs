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
    public int IdleTimeoutMinutes { get; } = 10;

    public ApiService()
    {
        var baseUrl = AppConstants.DefaultApiBaseUrl;
        var idleTimeout = 10;
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiSettings.json");
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(AppConstants.ApiBaseUrlSetting, out var prop))
                {
                    var val = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        baseUrl = val;
                    }
                }
                if (doc.RootElement.TryGetProperty("IdleTimeoutMinutes", out var idleProp))
                {
                    if (idleProp.ValueKind == JsonValueKind.Number)
                    {
                        idleTimeout = idleProp.GetInt32();
                    }
                }
            }
            else
            {
                var defaultJson = JsonSerializer.Serialize(new { BaseUrl = AppConstants.DefaultApiBaseUrl, IdleTimeoutMinutes = 10 }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, defaultJson);
            }
        }
        catch
        {
            // Fallback to default
        }

        Helpers.ClientLogger.Log($"[DIAGNOSTIC] ApiService initialized with API URL: {baseUrl}");
        BaseUrl = baseUrl;
        IdleTimeoutMinutes = idleTimeout;
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
            Helpers.ClientLogger.Log("[DIAGNOSTIC] JSON deserialization failed", ex);
            throw new InvalidOperationException("Failed to process the server response.", ex);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] GET Request URL: {requestUrl}");
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] GET Response code for {endpoint}: {(int)response.StatusCode} ({response.StatusCode})");
            response.EnsureSuccessStatusCode();
            return await ReadContentSafeAsync<T>(response.Content);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] GET request failed for endpoint: {endpoint}. Error: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<byte[]> GetByteArrayAsync(string endpoint)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] GET Byte Array Request URL: {requestUrl}");
        try
        {
            var data = await _httpClient.GetByteArrayAsync(endpoint);
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] GET Byte Array Success for {endpoint}");
            return data;
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] GET byte array request failed for endpoint: {endpoint}. Error: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] POST Request URL: {requestUrl}");
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, body);
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] POST Response code for {endpoint}: {(int)response.StatusCode} ({response.StatusCode})");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return await ReadContentSafeAsync<TResponse>(response.Content);
            }
            response.EnsureSuccessStatusCode();
            return await ReadContentSafeAsync<TResponse>(response.Content);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] POST request failed for endpoint: {endpoint}. Error: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<string> PutAsync<TRequest>(string endpoint, TRequest body)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] PUT Request URL: {requestUrl}");
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, body);
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] PUT Response code for {endpoint}: {(int)response.StatusCode} ({response.StatusCode})");
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var contentStr = await response.Content.ReadAsStringAsync();
                throw new ConcurrencyException(contentStr);
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] PUT request failed for endpoint: {endpoint}. Error: {ex.Message}", ex);
            throw;
        }
    }

    public class ConcurrencyException : Exception
    {
        public string ServerJson { get; }
        public ConcurrencyException(string serverJson) : base("A concurrency conflict occurred.")
        {
            ServerJson = serverJson;
        }
    }

    public async Task DeleteAsync(string endpoint)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] DELETE Request URL: {requestUrl}");
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] DELETE Response code for {endpoint}: {(int)response.StatusCode} ({response.StatusCode})");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] DELETE request failed for endpoint: {endpoint}. Error: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<TResponse?> UploadFileAsync<TResponse>(string endpoint, string filePath)
    {
        var requestUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
        Helpers.ClientLogger.Log($"[DIAGNOSTIC] FileUpload Request URL: {requestUrl}, file: {filePath}");
        try
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync(endpoint, content);
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] FileUpload Response code for {endpoint}: {(int)response.StatusCode} ({response.StatusCode})");
            response.EnsureSuccessStatusCode();
            return await ReadContentSafeAsync<TResponse>(response.Content);
        }
        catch (Exception ex)
        {
            Helpers.ClientLogger.Log($"[DIAGNOSTIC] FileUpload request failed for endpoint: {endpoint}, file: {filePath}. Error: {ex.Message}", ex);
            throw;
        }
    }
}
