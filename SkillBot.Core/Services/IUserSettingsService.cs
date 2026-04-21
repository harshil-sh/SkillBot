namespace SkillBot.Core.Services;

/// <summary>
/// Settings data returned to callers; avoids exposing the full User record.
/// </summary>
public class UserSettings
{
    public string PreferredProvider { get; set; } = "openai";
    public bool HasOpenAiKey { get; set; }
    public bool HasClaudeKey { get; set; }
    public bool HasGeminiKey { get; set; }
    public bool HasSerpApiKey { get; set; }
}

/// <summary>
/// Manages per-user LLM provider and API-key preferences.
/// </summary>
public interface IUserSettingsService
{
    Task<UserSettings> GetSettingsAsync(string userId);
    Task UpdateApiKeyAsync(string userId, string provider, string apiKey);
    Task UpdateProviderAsync(string userId, string provider);
}
