namespace SkillBot.Console.Services;

public interface IConsoleAdminService
{
    Task<string> GetUsageStatsAsync();
    Task<string> GetConversationStatsAsync(string conversationId);
    Task<string> GetTopConversationsAsync(int limit = 10);
    Task ResetStatsAsync();
    Task<string> GetCacheStatsAsync();
    Task<string> GetCacheHealthAsync();
    Task ClearCacheAsync();
    Task InvalidateCacheAsync(string pattern);
    Task<string> ListUsersAsync();
    Task<string> GetHealthAsync();
    Task<string> GetAgentsAsync();
}
