namespace SkillBot.Console.Services;

public interface IConsoleAdminService
{
    Task<string> GetUsageStatsAsync();
    Task<string> GetCacheStatsAsync();
    Task<string> ListUsersAsync();
    Task<string> GetHealthAsync();
}
