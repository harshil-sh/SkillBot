using SkillBot.Web.Services.Models;
namespace SkillBot.Web.Services;

public interface ISkillBotApiClient
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<ChatResponse> SendMessageAsync(ChatRequest request);
    Task<ChatResponse> SendMultiAgentMessageAsync(ChatRequest request);
    Task<List<ConversationSummary>> GetConversationsAsync();
    Task<List<ChatMessage>> GetConversationMessagesAsync(string conversationId);
    Task DeleteConversationAsync(string conversationId);
    Task UpdateApiKeyAsync(UpdateApiKeyRequest request);
    Task UpdateProviderAsync(UpdateProviderRequest request);

    // Admin
    Task<List<AdminUserResponse>> GetAdminUsersAsync();
    Task<AdminStatsResponse> GetAdminStatsAsync();
    Task DeleteAdminUserAsync(string userId);
    Task<HealthCheckResponse?> GetHealthAsync();

    // Tasks
    Task<ScheduleTaskResponse> ScheduleTaskAsync(ScheduleTaskRequest request);
    Task<ScheduleTaskResponse> ScheduleRecurringTaskAsync(ScheduleRecurringTaskRequest request);
    Task<ScheduledTaskInfo> GetTaskAsync(string taskId);
    Task<List<ScheduledTaskInfo>> GetAllTasksAsync();
    Task CancelTaskAsync(string taskId);
}
