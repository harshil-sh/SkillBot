# SkillBot Web — API Integration

This document covers how `SkillBot.Web` communicates with the `SkillBot.Api` backend: the `ISkillBotApiClient` interface, `HttpClient` configuration, JWT authorization, error handling, and all request/response models.

---

## Table of Contents

1. [Configuration](#1-configuration)
2. [ISkillBotApiClient Interface](#2-iskillbotapiclient-interface)
3. [JWT Authorization Header](#3-jwt-authorization-header)
4. [Error Handling](#4-error-handling)
5. [Request / Response Models](#5-request--response-models)
6. [Code Examples](#6-code-examples)
7. [CORS Considerations](#7-cors-considerations)

---

## 1. Configuration

### `wwwroot/appsettings.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7101"
  }
}
```

This file is downloaded by the browser at startup. In production you replace `BaseUrl` with your deployed API URL (e.g., `https://api.skillbot.example.com`).

### `Program.cs` wiring

```csharp
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddScoped<ISkillBotApiClient, SkillBotApiClient>();
```

All `HttpClient` instances are created from the single scoped registration. `SkillBotApiClient` receives it via constructor injection. The `BaseAddress` means relative URLs like `/api/chat` resolve to `https://localhost:7101/api/chat`.

---

## 2. ISkillBotApiClient Interface

```csharp
/// <summary>
/// Abstracts all HTTP calls to the SkillBot REST API.
/// Inject this interface in pages and components instead of using HttpClient directly.
/// </summary>
public interface ISkillBotApiClient
{
    // ── Authentication ────────────────────────────────────────────────────────

    /// <summary>Authenticate with email + password; returns a JWT on success.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Register a new user account.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    // ── Chat ──────────────────────────────────────────────────────────────────

    /// <summary>Send a chat message to the single-agent engine.</summary>
    Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>Send a task to the multi-agent orchestrator.</summary>
    Task<ChatResponse> SendMultiAgentMessageAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>Retrieve all conversations for the authenticated user.</summary>
    Task<IEnumerable<ConversationDto>> GetConversationsAsync(CancellationToken ct = default);

    /// <summary>Retrieve the full message history for a specific conversation.</summary>
    Task<ConversationDetailDto> GetConversationAsync(string conversationId, CancellationToken ct = default);

    /// <summary>Delete a conversation and all its messages.</summary>
    Task DeleteConversationAsync(string conversationId, CancellationToken ct = default);

    // ── Search ────────────────────────────────────────────────────────────────

    /// <summary>Perform a web search via the SerpAPI plugin (requires SerpApi key configured).</summary>
    Task<SearchResponse> SearchAsync(string query, CancellationToken ct = default);

    // ── User Settings ─────────────────────────────────────────────────────────

    /// <summary>Fetch the authenticated user's settings (API keys, provider preference, etc.).</summary>
    Task<UserSettingsDto> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>Persist updated user settings.</summary>
    Task UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken ct = default);

    // ── User Profile ──────────────────────────────────────────────────────────

    /// <summary>Fetch the authenticated user's profile.</summary>
    Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default);

    /// <summary>Update display name or other profile fields.</summary>
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>Change the authenticated user's password.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>Permanently delete the authenticated user's account.</summary>
    Task DeleteAccountAsync(CancellationToken ct = default);

    // ── Admin ─────────────────────────────────────────────────────────────────

    /// <summary>Retrieve aggregate stats for the admin dashboard.</summary>
    Task<AdminStatsDto> GetAdminStatsAsync(CancellationToken ct = default);

    /// <summary>Paginated list of all users.</summary>
    Task<PagedResult<AdminUserDto>> GetUsersAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>Update a user's role or active status.</summary>
    Task UpdateUserAsync(string userId, AdminUserUpdateRequest request, CancellationToken ct = default);

    /// <summary>Retrieve usage analytics data.</summary>
    Task<AnalyticsDto> GetAnalyticsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    // ── Health ────────────────────────────────────────────────────────────────

    /// <summary>Get the API version string.</summary>
    Task<string> GetApiVersionAsync(CancellationToken ct = default);
}
```

---

## 3. JWT Authorization Header

Every request to a protected endpoint must include `Authorization: Bearer <token>`.

`SkillBotApiClient` attaches the token automatically using a private helper:

```csharp
public class SkillBotApiClient : ISkillBotApiClient
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public SkillBotApiClient(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    /// <summary>Reads the JWT from localStorage and attaches it as an Authorization header.</summary>
    private async Task AttachTokenAsync()
    {
        var token = await _js.InvokeAsync<string?>("localStorage.getItem", "skillbot_jwt");
        _http.DefaultRequestHeaders.Authorization = token is not null
            ? new AuthenticationHeaderValue("Bearer", token)
            : null;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken ct = default)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Empty response from /api/chat");
    }

    // ... other methods follow the same pattern
}
```

> **Security note:** `localStorage` is accessible to any JavaScript on the page. Ensure the Content-Security-Policy disallows inline scripts. See [DEPLOYMENT_WEB.md](../DEPLOYMENT_WEB.md) for the recommended CSP header.

---

## 4. Error Handling

All API methods throw `HttpRequestException` on non-2xx responses. Pages catch this and display user-friendly messages via MudBlazor's `ISnackbar`.

### Standard try/catch pattern

```razor
@inject ISnackbar Snackbar

@code {
    private async Task SendMessageAsync()
    {
        _isLoading = true;
        try
        {
            var response = await ApiClient.SendMessageAsync(new ChatRequest(_inputText));
            _messages.Add(/* ... */);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token expired or invalid — redirect to login
            NavigationManager.NavigateTo("/login?returnUrl=/chat");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            Snackbar.Add("Rate limit reached. Please wait a moment.", Severity.Warning);
        }
        catch (HttpRequestException ex)
        {
            Snackbar.Add($"API error: {ex.Message}", Severity.Error);
        }
        catch (TaskCanceledException)
        {
            Snackbar.Add("Request timed out. Please try again.", Severity.Warning);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### HTTP Status → User Message Mapping

| HTTP Status | Meaning | User-facing message |
|-------------|---------|---------------------|
| 400 Bad Request | Validation error | Show field-level error from response body |
| 401 Unauthorized | Token missing or expired | Redirect to `/login` |
| 403 Forbidden | Insufficient role | "You don't have permission to do that." |
| 404 Not Found | Resource deleted / invalid ID | "That resource no longer exists." |
| 422 Unprocessable | Business logic rejection | Show API error message |
| 429 Too Many Requests | Rate limit hit | "Too many requests. Try again in a moment." |
| 500 Internal Server Error | Server-side bug | "Something went wrong on our end. Try again." |
| Network error (no status) | API unreachable | "Cannot connect to SkillBot API. Check your connection." |

### Deserializing error details

The API returns problem-detail–style errors:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
```

```csharp
if (!response.IsSuccessStatusCode)
{
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    // surface problem.Title or individual problem.Extensions["errors"]
}
```

---

## 5. Request / Response Models

### Authentication

```csharp
// POST /api/auth/login
public record LoginRequest(
    string Email,
    string Password
);

// POST /api/auth/register
public record RegisterRequest(
    string Username,
    string Email,
    string Password
);

// Response for both login and register
public record AuthResponse(
    string Token,
    DateTime Expiry,
    string UserId,
    string Username,
    string Email,
    string Role              // "User" | "Admin"
);
```

### Chat

```csharp
// POST /api/chat  |  POST /api/multi-agent/chat
public record ChatRequest(
    string Message,
    string? ConversationId = null   // null = start new conversation
);

public record ChatResponse(
    string Reply,
    string ConversationId,
    int InputTokens,
    int OutputTokens,
    string Provider,                // "openai" | "claude" | "gemini"
    string Model
);

// GET /api/conversations
public record ConversationDto(
    string Id,
    string Title,             // First user message, truncated to 60 chars
    DateTime CreatedAt,
    DateTime LastMessageAt,
    int MessageCount
);

// GET /api/conversations/{id}
public record ConversationDetailDto(
    string Id,
    string Title,
    DateTime CreatedAt,
    IReadOnlyList<ChatMessageDto> Messages
);

public record ChatMessageDto(
    string Role,              // "user" | "assistant"
    string Content,
    DateTime Timestamp,
    int? TokenCount
);
```

### Search

```csharp
// POST /api/search
public record SearchRequest(string Query);

public record SearchResponse(
    string Query,
    IReadOnlyList<SearchResult> Results
);

public record SearchResult(
    string Title,
    string Url,
    string Snippet
);
```

### User Settings

```csharp
// GET /api/settings  |  PUT /api/settings
public record UserSettingsDto(
    string? OpenAiApiKey,       // masked: "sk-...xxxx" or null
    string? ClaudeApiKey,
    string? GeminiApiKey,
    string PreferredProvider,   // "openai" | "claude" | "gemini"
    string? PreferredModel,
    string? SystemPrompt,
    bool SaveHistory,
    int HistoryRetentionDays    // 7 | 30 | 365 | -1 (forever)
);

public record UpdateSettingsRequest(
    string? OpenAiApiKey,
    string? ClaudeApiKey,
    string? GeminiApiKey,
    string? PreferredProvider,
    string? PreferredModel,
    string? SystemPrompt,
    bool? SaveHistory,
    int? HistoryRetentionDays
);
```

### User Profile

```csharp
// GET /api/profile
public record UserProfileDto(
    string UserId,
    string Username,
    string Email,
    string Role,
    DateTime CreatedAt,
    int TotalMessages,
    int TotalTokens
);

public record UpdateProfileRequest(string? Username);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
```

### Admin

```csharp
// GET /api/admin/stats
public record AdminStatsDto(
    int TotalUsers,
    int ActiveUsersToday,
    long TotalMessages,
    long TotalTokensConsumed,
    double CacheHitRate,
    IReadOnlyList<RecentActivityDto> RecentActivity
);

public record RecentActivityDto(
    string UserId,
    string Username,
    string Action,
    DateTime Timestamp
);

// GET /api/admin/users
public record AdminUserDto(
    string UserId,
    string Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    int TotalMessages
);

public record AdminUserUpdateRequest(
    string? Role,
    bool? IsActive
);

// GET /api/admin/analytics
public record AnalyticsDto(
    IReadOnlyList<DailyMetric> MessagesPerDay,
    IReadOnlyList<ProviderUsage> TokensByProvider,
    IReadOnlyList<DailyMetric> CacheHitRatePerDay
);

public record DailyMetric(DateTimeOffset Date, double Value);
public record ProviderUsage(string Provider, long Tokens);

// Generic paginated result
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
```

---

## 6. Code Examples

### Login

```csharp
var response = await ApiClient.LoginAsync(new LoginRequest("alice@example.com", "Secret123!"));
await AuthStateProvider.MarkUserAsAuthenticated(response.Token);
NavigationManager.NavigateTo("/chat");
```

### Send a chat message

```csharp
var response = await ApiClient.SendMessageAsync(new ChatRequest(
    Message: _inputText,
    ConversationId: _currentConversationId
));
_messages.Add(new ChatMessageDto("assistant", response.Reply, DateTime.UtcNow, response.OutputTokens));
_currentConversationId = response.ConversationId;
```

### Load conversation history

```csharp
var detail = await ApiClient.GetConversationAsync(conversationId);
_messages = detail.Messages.ToList();
_currentConversationId = detail.Id;
```

### Update API keys

```csharp
await ApiClient.UpdateSettingsAsync(new UpdateSettingsRequest(
    OpenAiApiKey: _openAiKey,
    ClaudeApiKey: _claudeKey,
    GeminiApiKey: _geminiKey,
    PreferredProvider: _selectedProvider,
    PreferredModel: null,
    SystemPrompt: null,
    SaveHistory: null,
    HistoryRetentionDays: null
));
Snackbar.Add("API keys saved!", Severity.Success);
```

### Fetch admin stats

```csharp
protected override async Task OnInitializedAsync()
{
    _stats = await ApiClient.GetAdminStatsAsync();
}
```

---

## 7. CORS Considerations

The `SkillBot.Api` must allow cross-origin requests from the web UI's origin. In `SkillBot.Api/Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebUI", policy =>
        policy.WithOrigins("http://localhost:5000", "https://skillbot.example.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ...
app.UseCors("WebUI");
```

In development, the Blazor DevServer proxies requests automatically, so CORS issues are only encountered when the API and web UI run on different origins (production deployment or separate ports).

---

*See also: [ARCHITECTURE.md](ARCHITECTURE.md) · [STATE_MANAGEMENT.md](STATE_MANAGEMENT.md)*
