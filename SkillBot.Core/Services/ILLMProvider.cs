namespace SkillBot.Core.Services;

public interface ILLMProvider
{
    string Name { get; }
    bool RequiresApiKey { get; }
    Task<string> GenerateResponseAsync(string prompt, string? apiKey = null);
}
