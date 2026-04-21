namespace SkillBot.Api.Models.Settings;

public class UpdateApiKeyRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class UpdateProviderRequest
{
    public string Provider { get; set; } = string.Empty;
}

public class UserSettingsResponse
{
    public string PreferredProvider { get; set; } = string.Empty;
    public bool HasOpenAiKey { get; set; }
    public bool HasClaudeKey { get; set; }
    public bool HasGeminiKey { get; set; }
    public bool HasSerpApiKey { get; set; }
}
