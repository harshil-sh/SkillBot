using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class AdminCommands
{
    public static async Task HandleStatsAsync(IConsoleAdminService adminService)
    {
        try
        {
            var result = await adminService.GetUsageStatsAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleCacheStatsAsync(IConsoleAdminService adminService)
    {
        try
        {
            var result = await adminService.GetCacheStatsAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleUsersAsync(IConsoleAdminService adminService)
    {
        try
        {
            var result = await adminService.ListUsersAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleHealthAsync(IConsoleAdminService adminService)
    {
        try
        {
            var result = await adminService.GetHealthAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }
}
