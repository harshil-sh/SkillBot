using System.Text.Json.Serialization;

namespace SkillBot.Console.Services;

public class ConsoleSettingsService : IConsoleSettingsService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _settingsFilePath;
    private readonly ApiClient _apiClient;

    public ConsoleSettingsService(ApiClient apiClient)
    {
        _apiClient = apiClient;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDirectory = Path.Combine(appDataPath, "SkillBot");
        Directory.CreateDirectory(settingsDirectory);

        _settingsFilePath = Path.Combine(settingsDirectory, "console-settings.json");
        LoadFromDisk();
    }

    public Task<string> GetSettingAsync(string key)
    {
        lock (_sync)
        {
            if (_settings.TryGetValue(key, out var value))
                return Task.FromResult(value);
        }

        throw new KeyNotFoundException($"Setting '{key}' not found.");
    }

    public Task SetSettingAsync(string key, string value)
    {
        lock (_sync)
        {
            _settings[key] = value;
            SaveToDisk();
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>> ListSettingsAsync()
    {
        lock (_sync)
        {
            return Task.FromResult(new Dictionary<string, string>(_settings, StringComparer.OrdinalIgnoreCase));
        }
    }

    public async Task SetApiKeyAsync(string provider, string apiKey)
    {
        await _apiClient.PutAsync("/api/settings/api-key", new { provider, apiKey });
    }

    public async Task SetProviderAsync(string provider)
    {
        await _apiClient.PutAsync("/api/settings/provider", new { provider });
    }

    public async Task<UserSettingsResult> GetUserSettingsAsync()
    {
        var response = await _apiClient.GetAsync<ApiUserSettingsResponse>("/api/settings")
            ?? throw new InvalidOperationException("Failed to retrieve user settings.");

        return new UserSettingsResult
        {
            PreferredProvider = response.PreferredProvider,
            HasOpenAiKey      = response.HasOpenAiKey,
            HasClaudeKey      = response.HasClaudeKey,
            HasGeminiKey      = response.HasGeminiKey,
            HasTelegramToken  = response.HasTelegramToken,
            HasSerpApiKey     = response.HasSerpApiKey
        };
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (loaded == null)
            {
                return;
            }

            foreach (var (key, value) in loaded)
            {
                _settings[key] = value;
            }
        }
        catch
        {
            // Ignore invalid settings file and continue with in-memory defaults.
        }
    }

    private void SaveToDisk()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            _settings,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(_settingsFilePath, json);
    }

    private sealed class ApiUserSettingsResponse
    {
        [JsonPropertyName("preferredProvider")] public required string PreferredProvider { get; init; }
        [JsonPropertyName("hasOpenAiKey")]      public required bool HasOpenAiKey { get; init; }
        [JsonPropertyName("hasClaudeKey")]      public required bool HasClaudeKey { get; init; }
        [JsonPropertyName("hasGeminiKey")]      public required bool HasGeminiKey { get; init; }
        [JsonPropertyName("hasTelegramToken")]  public required bool HasTelegramToken { get; init; }
        [JsonPropertyName("hasSerpApiKey")]     public required bool HasSerpApiKey { get; init; }
    }
}
