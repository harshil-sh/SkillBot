using Microsoft.Extensions.DependencyInjection;
using SkillBot.Console.Commands;
using SkillBot.Console.Helpers;

namespace SkillBot.Console.Services;

public class CommandRouter
{
    public async Task ExecuteAsync(CommandResult command, IServiceProvider services)
    {
        try
        {
            await ExecuteCommandAsync(command, services);
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode == 429)
        {
            ConsoleHelper.WriteWarning("Rate limit exceeded. Please wait before trying again.");
        }
    }

    private async Task ExecuteCommandAsync(CommandResult command, IServiceProvider services)
    {
        switch (command.Command)
        {
            // ── Auth ──────────────────────────────────────────────────────────
            case "register":
                await AuthCommands.HandleRegisterAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleAuthService>(),
                    services.GetRequiredService<IConsoleSettingsService>());
                break;

            case "login":
                await AuthCommands.HandleLoginAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleAuthService>());
                break;

            case "logout":
                await AuthCommands.HandleLogoutAsync(
                    services.GetRequiredService<IConsoleAuthService>());
                break;

            // ── Chat ──────────────────────────────────────────────────────────
            case "chat":
                await ChatCommands.HandleChatAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleChatService>());
                break;

            case "multi-agent":
                await ChatCommands.HandleMultiAgentChatAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleChatService>());
                break;

            case "history":
                await ChatHistoryCommands.HandleHistoryAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleChatService>());
                break;

            case "conversation":
                await ChatHistoryCommands.HandleGetConversationAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleChatService>());
                break;

            case "delete-conversation":
                await ChatHistoryCommands.HandleDeleteConversationAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleChatService>());
                break;

            // ── Search ────────────────────────────────────────────────────────
            case "search":
                await SearchCommands.HandleSearchAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleSearchService>());
                break;

            case "search-news":
                await SearchCommands.HandleSearchNewsAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleSearchService>());
                break;

            // ── Settings ──────────────────────────────────────────────────────
            case "settings":
                await HandleSettingsSubcommandAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleSettingsService>());
                break;

            // ── Plugins ───────────────────────────────────────────────────────
            case "plugins":
                await PluginCommands.HandlePluginsAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsolePluginService>());
                break;

            // ── Tasks ─────────────────────────────────────────────────────────
            case "tasks":
                await TasksCommands.HandleTasksAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleTaskService>());
                break;

            // ── Usage / Stats ─────────────────────────────────────────────────
            case "stats":
                await AdminCommands.HandleStatsAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "stats-conversation":
                await AdminCommands.HandleConversationStatsAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "top-conversations":
                await AdminCommands.HandleTopConversationsAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "reset-stats":
                await AdminCommands.HandleResetStatsAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            // ── Cache ─────────────────────────────────────────────────────────
            case "cache-stats":
                await AdminCommands.HandleCacheStatsAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "cache-health":
                await AdminCommands.HandleCacheHealthAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "cache-clear":
                await AdminCommands.HandleCacheClearAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "cache-invalidate":
                await AdminCommands.HandleCacheInvalidateAsync(
                    command.Arguments,
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            // ── Multi-agent info ──────────────────────────────────────────────
            case "agents":
                await AdminCommands.HandleAgentsAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            // ── Admin misc ────────────────────────────────────────────────────
            case "users":
                await AdminCommands.HandleUsersAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "health":
                await AdminCommands.HandleHealthAsync(
                    services.GetRequiredService<IConsoleAdminService>());
                break;

            case "help":
                HelpCommands.ShowHelp();
                break;

            case "exit":
                break;

            default:
                ConsoleHelper.WriteError($"❌ Unknown command: '{command.Command}'. Type 'help' for available commands.");
                break;
        }
    }

    private static async Task HandleSettingsSubcommandAsync(
        Dictionary<string, string> args,
        IConsoleSettingsService settingsService)
    {
        if (!args.TryGetValue("0", out var sub))
        {
            PrintSettingsUsage();
            return;
        }

        // Shift positional args so handlers see key/value at "0"/"1"
        var shifted = ShiftPositional(args);

        switch (sub.ToLowerInvariant())
        {
            case "get":
                await SettingsCommands.HandleGetSettingAsync(shifted, settingsService);
                break;
            case "set":
                await SettingsCommands.HandleSetSettingAsync(shifted, settingsService);
                break;
            case "list":
                await SettingsCommands.HandleListSettingsAsync(settingsService);
                break;
            case "set-api-key":
                await SettingsCommands.HandleSetApiKeyAsync(args, settingsService);
                break;
            case "set-provider":
                await SettingsCommands.HandleSetProviderAsync(shifted, settingsService);
                break;
            case "show":
                await SettingsCommands.HandleGetSettingsAsync(settingsService);
                break;
            default:
                PrintSettingsUsage();
                break;
        }
    }

    // Decrements positional keys by 1, preserving named flags unchanged.
    private static Dictionary<string, string> ShiftPositional(Dictionary<string, string> args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in args)
        {
            if (int.TryParse(key, out var index))
            {
                if (index > 0)
                    result[(index - 1).ToString()] = value;
            }
            else
            {
                result[key] = value;
            }
        }
        return result;
    }

    private static void PrintSettingsUsage()
    {
        System.Console.WriteLine("Usage: settings <subcommand> [args]");
        System.Console.WriteLine("  settings get <key>");
        System.Console.WriteLine("  settings set <key> <value>");
        System.Console.WriteLine("  settings list");
        System.Console.WriteLine("  settings show");
        System.Console.WriteLine("  settings set-api-key --provider <openai|claude|gemini|serpapi|telegram> --key <api-key>");
        System.Console.WriteLine("  settings set-provider <openai|claude|gemini>");
    }
}
