using SkillBot.Core.Services;
using SkillBot.Infrastructure.Repositories;

namespace SkillBot.Api.Services;

public class UserSettingsService : IUserSettingsService
{
    // LLM providers — valid for both api-key and preferred-provider endpoints
    private static readonly HashSet<string> ValidProviders = ["openai", "claude", "gemini"];

    // Extended set for api-key updates — includes search keys
    private static readonly HashSet<string> ValidApiKeyProviders =
        [..ValidProviders, "serpapi"];

    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(IUserRepository userRepository, ILogger<UserSettingsService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserSettings> GetSettingsAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return new UserSettings
        {
            PreferredProvider = user.PreferredProvider,
            HasOpenAiKey      = !string.IsNullOrEmpty(user.OpenAiApiKey),
            HasClaudeKey      = !string.IsNullOrEmpty(user.ClaudeApiKey),
            HasGeminiKey      = !string.IsNullOrEmpty(user.GeminiApiKey),
            HasSerpApiKey     = !string.IsNullOrEmpty(user.SerpApiKey)
        };
    }

    public async Task UpdateApiKeyAsync(string userId, string provider, string apiKey)
    {
        ValidateApiKeyProvider(provider);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var updated = provider.ToLowerInvariant() switch
        {
            "openai"  => user with { OpenAiApiKey = apiKey },
            "claude"  => user with { ClaudeApiKey = apiKey },
            "gemini"  => user with { GeminiApiKey = apiKey },
            "serpapi" => user with { SerpApiKey   = apiKey },
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        await _userRepository.UpdateAsync(updated);
        _logger.LogInformation("Updated {Provider} key for user {UserId}.", provider, userId);
    }

    public async Task UpdateProviderAsync(string userId, string provider)
    {
        ValidateLlmProvider(provider);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        await _userRepository.UpdateAsync(user with { PreferredProvider = provider.ToLowerInvariant() });
        _logger.LogInformation("Updated preferred provider to {Provider} for user {UserId}.", provider, userId);
    }

    private static void ValidateApiKeyProvider(string provider)
    {
        if (!ValidApiKeyProviders.Contains(provider.ToLowerInvariant()))
            throw new ArgumentException(
                $"Invalid provider '{provider}'. Must be one of: {string.Join(", ", ValidApiKeyProviders)}.");
    }

    private static void ValidateLlmProvider(string provider)
    {
        if (!ValidProviders.Contains(provider.ToLowerInvariant()))
            throw new ArgumentException(
                $"Invalid provider '{provider}'. Must be one of: {string.Join(", ", ValidProviders)}.");
    }
}
