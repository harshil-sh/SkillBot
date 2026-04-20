using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class PluginCommands
{
    public static async Task HandlePluginsAsync(Dictionary<string, string> args, IConsolePluginService pluginService)
    {
        // If a name is provided as first positional arg, show that plugin; otherwise list all
        if (args.TryGetValue("0", out var name) && !string.IsNullOrWhiteSpace(name))
        {
            try
            {
                var result = await pluginService.GetPluginAsync(name);
                System.Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
            }
        }
        else
        {
            try
            {
                var result = await pluginService.GetPluginsAsync();
                System.Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
            }
        }
    }
}
