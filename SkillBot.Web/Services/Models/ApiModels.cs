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
public record MultiAgentRequest(string Task, string? ConversationId = null);
public record MultiAgentResponse(string FinalResponse, string ConversationId, string Strategy, double TotalExecutionTimeMs);

// Admin models
public record AdminUserResponse(string Id, string Username, string Email, DateTime CreatedAt, DateTime? LastActive, bool IsActive);
public record AdminStatsResponse(int TotalUsers, int TotalConversations, long TotalTokensUsed);
public record HealthCheckEntry(string Status, string? Description = null);
public record HealthCheckResponse(string Status, Dictionary<string, HealthCheckEntry>? Entries = null);

// Task models
public record ScheduledTaskInfo(
    string TaskId,
    string Task,
    bool IsMultiAgent,
    bool IsRecurring,
    string? CronExpression,
    DateTime? ScheduledFor,
    string Status,
    string? Result,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public record ScheduleTaskRequest(string Task, DateTime ExecuteAt, bool IsMultiAgent = false);
public record ScheduleRecurringTaskRequest(string Task, string CronExpression, bool IsMultiAgent = false);
public record ScheduleTaskResponse(string TaskId, string Message);
