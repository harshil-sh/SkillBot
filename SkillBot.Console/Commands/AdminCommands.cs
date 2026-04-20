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

    public static async Task HandleConversationStatsAsync(Dictionary<string, string> args, IConsoleAdminService adminService)
    {
        if (!args.TryGetValue("0", out var id) || string.IsNullOrWhiteSpace(id))
        {
            ConsoleHelper.WriteError("Conversation ID is required. Usage: stats-conversation <id>");
            return;
        }

        try
        {
            var result = await adminService.GetConversationStatsAsync(id);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleTopConversationsAsync(Dictionary<string, string> args, IConsoleAdminService adminService)
    {
        var limit = 10;
        if (args.TryGetValue("limit", out var limitStr) && int.TryParse(limitStr, out var l))
            limit = l;

        try
        {
            var result = await adminService.GetTopConversationsAsync(limit);
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleResetStatsAsync(IConsoleAdminService adminService)
    {
        try
        {
            await adminService.ResetStatsAsync();
            ConsoleHelper.WriteSuccess("✅ Usage statistics reset.");
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

    public static async Task HandleCacheHealthAsync(IConsoleAdminService adminService)
    {
        try
        {
            var result = await adminService.GetCacheHealthAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleCacheClearAsync(IConsoleAdminService adminService)
    {
        try
        {
            await adminService.ClearCacheAsync();
            ConsoleHelper.WriteSuccess("✅ Cache cleared.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleCacheInvalidateAsync(Dictionary<string, string> args, IConsoleAdminService adminService)
    {
        if (!args.TryGetValue("0", out var pattern) || string.IsNullOrWhiteSpace(pattern))
        {
            ConsoleHelper.WriteError("Pattern is required. Usage: cache-invalidate <pattern>  e.g. llm_response_*");
            return;
        }

        try
        {
            await adminService.InvalidateCacheAsync(pattern);
            ConsoleHelper.WriteSuccess($"✅ Cache entries matching '{pattern}' invalidated.");
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

    public static async Task HandleAgentsAsync(IConsoleAdminService adminService)
    {
        try
        {
            var result = await adminService.GetAgentsAsync();
            System.Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }
}

