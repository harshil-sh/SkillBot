using SkillBot.Console.Helpers;
using SkillBot.Console.Services;

namespace SkillBot.Console.Commands;

public static class AuthCommands
{
    public static async Task HandleRegisterAsync(
        Dictionary<string, string> args,
        IConsoleAuthService authService,
        IConsoleSettingsService settingsService)
    {
        if (!args.TryGetValue("0", out var email) || string.IsNullOrWhiteSpace(email))
        {
            ConsoleHelper.WriteError("Email is required. Usage: register <email> <password> <username>");
            return;
        }

        if (!args.TryGetValue("1", out var password) || string.IsNullOrWhiteSpace(password))
        {
            ConsoleHelper.WriteError("Password is required. Usage: register <email> <password> <username>");
            return;
        }

        if (!args.TryGetValue("2", out var username) || string.IsNullOrWhiteSpace(username))
        {
            ConsoleHelper.WriteError("Username is required. Usage: register <email> <password> <username>");
            return;
        }

        try
        {
            await authService.RegisterAsync(email, password, username);
            ConsoleHelper.WriteSuccess("✅ Registered successfully.");
            await RunApiKeySetupWizardAsync(settingsService);
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleLoginAsync(Dictionary<string, string> args, IConsoleAuthService authService)
    {
        if (!args.TryGetValue("0", out var identifier) || string.IsNullOrWhiteSpace(identifier))
        {
            ConsoleHelper.WriteError("Username or email is required. Usage: login <username-or-email>");
            return;
        }

        var password = ReadPasswordFromConsole();
        if (string.IsNullOrWhiteSpace(password))
        {
            ConsoleHelper.WriteError("Password is required.");
            return;
        }

        try
        {
            await authService.LoginAsync(identifier, password);
            ConsoleHelper.WriteSuccess("✅ Logged in successfully.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    public static async Task HandleLogoutAsync(IConsoleAuthService authService)
    {
        try
        {
            await authService.LogoutAsync();
            ConsoleHelper.WriteSuccess("✅ Logged out successfully.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
        }
    }

    private static async Task RunApiKeySetupWizardAsync(IConsoleSettingsService settingsService)
    {
        ConsoleHelper.WriteInfo("\nLet's configure your API keys (all saved securely to your account).");
        ConsoleHelper.WriteInfo("Press Enter to skip any key and set it later with 'settings api-key <provider> <key>'.\n");

        await PromptAndSaveApiKeyAsync(settingsService, "openai",   "Enter OpenAI API key");
        await PromptAndSaveApiKeyAsync(settingsService, "claude",   "Enter Anthropic Claude API key");
        await PromptAndSaveApiKeyAsync(settingsService, "gemini",   "Enter Google Gemini API key");
        await PromptAndSaveApiKeyAsync(settingsService, "serpapi",  "Enter SerpAPI key (for web search)");
        await PromptAndSaveApiKeyAsync(settingsService, "telegram", "Enter Telegram Bot Token");

        ConsoleHelper.WriteSuccess("✅ API key setup completed.");
    }

    private static async Task PromptAndSaveApiKeyAsync(
        IConsoleSettingsService settingsService,
        string provider,
        string prompt)
    {
        bool alreadySet = false;
        try
        {
            var current = await settingsService.GetUserSettingsAsync();
            alreadySet = provider switch
            {
                "openai"   => current.HasOpenAiKey,
                "claude"   => current.HasClaudeKey,
                "gemini"   => current.HasGeminiKey,
                "serpapi"  => current.HasSerpApiKey,
                "telegram" => current.HasTelegramToken,
                _          => false
            };
        }
        catch { /* settings not yet available — treat as not set */ }

        if (alreadySet)
            System.Console.Write($"{prompt} (already configured, press Enter to keep): ");
        else
            System.Console.Write($"{prompt}: ");

        var value = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(value))
        {
            if (alreadySet)
                ConsoleHelper.WriteInfo($"  Kept existing {provider} key.");
            else
                ConsoleHelper.WriteWarning($"  Skipped {provider}.");
            return;
        }

        try
        {
            await settingsService.SetApiKeyAsync(provider, value.Trim());
            ConsoleHelper.WriteSuccess($"  Saved {provider} key.");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"  Failed to save {provider} key: {ex.Message}");
        }
    }

    private static string ReadPasswordFromConsole()
    {
        if (System.Console.IsInputRedirected)
        {
            return System.Console.ReadLine() ?? string.Empty;
        }

        System.Console.Write("Password: ");

        var buffer = new System.Text.StringBuilder();
        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length -= 1;
                    System.Console.Write("\b \b");
                }

                continue;
            }

            if (char.IsControl(key.KeyChar))
            {
                continue;
            }

            buffer.Append(key.KeyChar);
            System.Console.Write("*");
        }

        return buffer.ToString();
    }
}
