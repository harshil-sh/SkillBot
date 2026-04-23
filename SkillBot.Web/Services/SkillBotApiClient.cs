using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using SkillBot.Web.Services.Models;

namespace SkillBot.Web.Services;

public class SkillBotApiClient : ISkillBotApiClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SkillBotApiClient(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<T> PostAsync<T>(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(url, body, _jsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            string errorMessage;
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                errorMessage = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() ?? errorContent
                    : doc.RootElement.TryGetProperty("error", out var err) ? err.GetString() ?? errorContent
                    : errorContent;
            }
            catch { errorMessage = errorContent; }
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }
        return (await response.Content.ReadFromJsonAsync<T>(_jsonOptions))!;
    }

    private async Task<T> GetAsync<T>(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            string errorMessage;
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                errorMessage = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() ?? errorContent
                    : doc.RootElement.TryGetProperty("error", out var err) ? err.GetString() ?? errorContent
                    : errorContent;
            }
            catch { errorMessage = errorContent; }
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }
        return (await response.Content.ReadFromJsonAsync<T>(_jsonOptions))!;
    }

    private async Task DeleteAsync(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorContent, null, response.StatusCode);
        }
    }

    private async Task PutAsync(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync(url, body, _jsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(errorContent, null, response.StatusCode);
        }
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
        PostAsync<AuthResponse>("/api/auth/register", request);

    public Task<AuthResponse> LoginAsync(LoginRequest request) =>
        PostAsync<AuthResponse>("/api/auth/login", request);

    public Task<ChatResponse> SendMessageAsync(ChatRequest request) =>
        PostAsync<ChatResponse>("/api/chat", request);

    public async Task<ChatResponse> SendMultiAgentMessageAsync(ChatRequest request)
    {
        var multiRequest = new MultiAgentRequest(request.Message, request.ConversationId);
        var response = await PostAsync<MultiAgentResponse>("/api/multi-agent/chat", multiRequest);
        return new ChatResponse(response.FinalResponse, response.ConversationId, DateTime.UtcNow);
    }

    public Task<List<ConversationSummary>> GetConversationsAsync() =>
        GetAsync<List<ConversationSummary>>("/api/conversations");

    public Task<List<ChatMessage>> GetConversationMessagesAsync(string conversationId) =>
        GetAsync<List<ChatMessage>>($"/api/conversations/{conversationId}/messages");

    public Task DeleteConversationAsync(string conversationId) =>
        DeleteAsync($"/api/conversations/{conversationId}");

    public Task UpdateApiKeyAsync(UpdateApiKeyRequest request) =>
        PutAsync("/api/settings/api-key", request);

    public Task UpdateProviderAsync(UpdateProviderRequest request) =>
        PutAsync("/api/settings/provider", request);

    public Task<List<AdminUserResponse>> GetAdminUsersAsync() =>
        GetAsync<List<AdminUserResponse>>("/api/admin/users");

    public Task<AdminStatsResponse> GetAdminStatsAsync() =>
        GetAsync<AdminStatsResponse>("/api/admin/stats");

    public Task DeleteAdminUserAsync(string userId) =>
        DeleteAsync($"/api/admin/users/{userId}");

    public Task<ScheduleTaskResponse> ScheduleTaskAsync(ScheduleTaskRequest request) =>
        PostAsync<ScheduleTaskResponse>("/api/tasks/schedule", request);

    public Task<ScheduleTaskResponse> ScheduleRecurringTaskAsync(ScheduleRecurringTaskRequest request) =>
        PostAsync<ScheduleTaskResponse>("/api/tasks/recurring", request);

    public Task<ScheduledTaskInfo> GetTaskAsync(string taskId) =>
        GetAsync<ScheduledTaskInfo>($"/api/tasks/{taskId}");

    public Task<List<ScheduledTaskInfo>> GetAllTasksAsync() =>
        GetAsync<List<ScheduledTaskInfo>>("/api/tasks");

    public Task CancelTaskAsync(string taskId) =>
        DeleteAsync($"/api/tasks/{taskId}");

    public async Task<HealthCheckResponse?> GetHealthAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _http.GetAsync("/api/health");
            if (!response.IsSuccessStatusCode) return new HealthCheckResponse("Unhealthy");
            return await response.Content.ReadFromJsonAsync<HealthCheckResponse>(_jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
