using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SkillBot.Console.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string? _authToken;

    public ApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ApiSettings:BaseUrl"]
            ?? configuration["ApiClient:BaseUrl"]
            ?? "http://localhost:5000";

        if (int.TryParse(configuration["ApiSettings:Timeout"], out var timeout))
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var request = BuildRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PostAsync<T>(string endpoint, object body)
    {
        var request = BuildRequest(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<string> GetStringAsync(string endpoint)
    {
        var request = BuildRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task PutAsync(string endpoint, object body)
    {
        var request = BuildRequest(HttpMethod.Put, endpoint);
        request.Content = JsonContent.Create(body);
        var response = await _httpClient.SendAsync(request);
        await EnsureSuccessOrThrowAsync(response);
    }

    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        var request = BuildRequest(HttpMethod.Delete, endpoint);
        var response = await _httpClient.SendAsync(request);
        await EnsureSuccessOrThrowAsync(response);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(errorBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    var message = messageElement.GetString();
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        throw new InvalidOperationException(message);
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to status-based error below if payload is not JSON.
            }
        }

        throw new HttpRequestException(
            $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string endpoint)
    {
        var url = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        var request = new HttpRequestMessage(method, url);

        if (_authToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        return request;
    }
}
