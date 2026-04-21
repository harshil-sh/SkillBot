namespace SkillBot.Web.Services.Models;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Username, string Email);
public record ChatRequest(string Message, string? ConversationId = null);
public record ChatResponse(string Message, string ConversationId, DateTime Timestamp);
public record ConversationSummary(string Id, string Title, string LastMessage, DateTime Timestamp);
public record ChatMessage(string Role, string Content, DateTime Timestamp);
public record UserSettingsResponse(string PreferredProvider, bool HasOpenAiKey, bool HasClaudeKey, bool HasGeminiKey);
public record UpdateApiKeyRequest(string Provider, string ApiKey);
public record UpdateProviderRequest(string Provider);

// Admin models
public record AdminUserResponse(string Id, string Username, string Email, DateTime CreatedAt, DateTime? LastActive, bool IsActive);
public record AdminStatsResponse(int TotalUsers, int TotalConversations, long TotalTokensUsed);
public record HealthCheckEntry(string Status, string? Description = null);
public record HealthCheckResponse(string Status, Dictionary<string, HealthCheckEntry>? Entries = null);
