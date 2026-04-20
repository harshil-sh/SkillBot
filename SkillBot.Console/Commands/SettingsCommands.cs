using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class SettingsCommands
{
    public static async Task HandleGetSettingAsync(Dictionary<string, string> args, IConsoleSettingsService settingsService)
    {
        if (!args.TryGetValue("0", out var key) || string.IsNullOrWhiteSpace(key))
        {
            ConsoleHelper.WriteError("Key is required. Usage: settings get <key>");
            return;
        }

        try
        {
            var value = await settingsService.GetSettingAsync(key);
            System.Console.WriteLine($"{key} = {value}");
        }
        catch (KeyNotFoundException)
        {
            ConsoleHelper.WriteWarning($"Setting '{key}' not found.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleSetSettingAsync(Dictionary<string, string> args, IConsoleSettingsService settingsService)
    {
        if (!args.TryGetValue("0", out var key) || string.IsNullOrWhiteSpace(key))
        {
            ConsoleHelper.WriteError("Key is required. Usage: settings set <key> <value>");
            return;
        }

        if (!args.TryGetValue("1", out var value) || string.IsNullOrWhiteSpace(value))
        {
            ConsoleHelper.WriteError("Value is required. Usage: settings set <key> <value>");
            return;
        }

        try
        {
            await settingsService.SetSettingAsync(key, value);
            ConsoleHelper.WriteSuccess($"✅ Set '{key}' successfully.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleListSettingsAsync(IConsoleSettingsService settingsService)
    {
        try
        {
            var settings = await settingsService.ListSettingsAsync();

            if (settings.Count == 0)
            {
                ConsoleHelper.WriteWarning("No settings configured.");
                return;
            }

            System.Console.WriteLine(new string('─', 40));
            foreach (var (key, value) in settings)
                System.Console.WriteLine($"  {key} = {value}");
            System.Console.WriteLine(new string('─', 40));
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleSetApiKeyAsync(Dictionary<string, string> args, IConsoleSettingsService settingsService)
    {
        if (!args.TryGetValue("provider", out var provider) || string.IsNullOrWhiteSpace(provider))
        {
            ConsoleHelper.WriteError("Provider is required. Usage: settings set-api-key --provider <openai|claude|gemini> --key <api-key>");
            return;
        }

        if (!args.TryGetValue("key", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            ConsoleHelper.WriteError("API key is required. Usage: settings set-api-key --provider <openai|claude|gemini> --key <api-key>");
            return;
        }

        try
        {
            await settingsService.SetApiKeyAsync(provider, apiKey);
            ConsoleHelper.WriteSuccess($"✅ API key for '{provider}' updated.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleSetProviderAsync(Dictionary<string, string> args, IConsoleSettingsService settingsService)
    {
        if (!args.TryGetValue("0", out var provider) || string.IsNullOrWhiteSpace(provider))
        {
            ConsoleHelper.WriteError("Provider is required. Usage: settings set-provider <openai|claude|gemini>");
            return;
        }

        try
        {
            await settingsService.SetProviderAsync(provider);
            ConsoleHelper.WriteSuccess($"✅ Preferred provider set to '{provider}'.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleGetSettingsAsync(IConsoleSettingsService settingsService)
    {
        try
        {
            var settings = await settingsService.GetUserSettingsAsync();

            System.Console.WriteLine(new string('─', 45));
            System.Console.WriteLine($"  Preferred provider : {settings.PreferredProvider}");
            System.Console.WriteLine($"  OpenAI key         : {(settings.HasOpenAiKey    ? "configured" : "not set")}");
            System.Console.WriteLine($"  Claude key         : {(settings.HasClaudeKey    ? "configured" : "not set")}");
            System.Console.WriteLine($"  Gemini key         : {(settings.HasGeminiKey    ? "configured" : "not set")}");
            System.Console.WriteLine($"  SerpAPI key        : {(settings.HasSerpApiKey   ? "configured" : "not set")}");
            System.Console.WriteLine($"  Telegram token     : {(settings.HasTelegramToken? "configured" : "not set")}");
            System.Console.WriteLine(new string('─', 45));
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }
}
