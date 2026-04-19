using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillBot.Plugins.Search;

namespace SkillBot.Console.Services;

public class ConsoleSearchService : IConsoleSearchService
{
    private readonly SerpApiPlugin _serpApiPlugin;

    public ConsoleSearchService(
        IConfiguration configuration,
        IConsoleSettingsService settingsService,
        ILogger<SerpApiPlugin> logger)
    {
        var serpApiKey = TryGetSetting(settingsService, "serpapi-api-key");
        if (string.IsNullOrWhiteSpace(serpApiKey))
        {
            _serpApiPlugin = new SerpApiPlugin(configuration, logger);
            return;
        }

        var mergedConfiguration = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SerpApi:ApiKey"] = serpApiKey
            })
            .Build();

        _serpApiPlugin = new SerpApiPlugin(mergedConfiguration, logger);
    }

    public Task<string> SearchWebAsync(string query, int count = 5)
        => _serpApiPlugin.SearchWebAsync(query, count);

    public Task<string> SearchNewsAsync(string query, int count = 5)
        => _serpApiPlugin.SearchNewsAsync(query, count);

    private static string? TryGetSetting(IConsoleSettingsService settingsService, string key)
    {
        try
        {
            return settingsService.GetSettingAsync(key).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}
