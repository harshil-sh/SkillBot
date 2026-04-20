namespace SkillBot.Console.Services;

public interface IConsoleSettingsService
{
    Task<string> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);
    Task<Dictionary<string, string>> ListSettingsAsync();
    Task SetApiKeyAsync(string provider, string apiKey);
    Task SetProviderAsync(string provider);
    Task<UserSettingsResult> GetUserSettingsAsync();
}

public sealed record UserSettingsResult
{
    public required string PreferredProvider { get; init; }
    public required bool HasOpenAiKey { get; init; }
    public required bool HasClaudeKey { get; init; }
    public required bool HasGeminiKey { get; init; }
    public required bool HasTelegramToken { get; init; }
    public required bool HasSerpApiKey { get; init; }
}
