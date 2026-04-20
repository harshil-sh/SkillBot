using System.Text;
using System.Text.Json.Serialization;

namespace SkillBot.Console.Services;

public class ConsolePluginService : IConsolePluginService
{
    private readonly ApiClient _apiClient;

    public ConsolePluginService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<string> GetPluginsAsync()
    {
        var plugins = await _apiClient.GetAsync<List<PluginResponse>>("/api/plugins");
        if (plugins is null || plugins.Count == 0)
            return "No plugins registered.";

        var sb = new StringBuilder();
        sb.AppendLine($"Registered plugins ({plugins.Count}):");
        sb.AppendLine(new string('─', 50));
        foreach (var p in plugins)
        {
            sb.AppendLine($"  {p.Name}  —  {p.Description}");
            sb.AppendLine($"    Functions ({p.Functions?.Count ?? 0}): {string.Join(", ", p.Functions?.Select(f => f.Name) ?? [])}");
        }
        sb.Append(new string('─', 50));
        return sb.ToString();
    }

    public async Task<string> GetPluginAsync(string pluginName)
    {
        var p = await _apiClient.GetAsync<PluginResponse>($"/api/plugins/{pluginName}");
        if (p is null)
            return $"Plugin '{pluginName}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Plugin: {p.Name}");
        sb.AppendLine($"Description: {p.Description}");
        sb.AppendLine(new string('─', 50));
        foreach (var f in p.Functions ?? [])
        {
            sb.AppendLine($"  {f.Name}  —  {f.Description}");
            foreach (var param in f.Parameters ?? [])
            {
                var req = param.IsRequired ? "*" : " ";
                sb.AppendLine($"    [{req}] {param.Name} ({param.Type}): {param.Description}");
            }
        }
        sb.Append(new string('─', 50));
        return sb.ToString();
    }

    private sealed class PluginResponse
    {
        [JsonPropertyName("name")]        public string Name { get; init; } = "";
        [JsonPropertyName("description")] public string Description { get; init; } = "";
        [JsonPropertyName("functions")]   public List<FunctionResponse>? Functions { get; init; }
    }

    private sealed class FunctionResponse
    {
        [JsonPropertyName("name")]        public string Name { get; init; } = "";
        [JsonPropertyName("description")] public string Description { get; init; } = "";
        [JsonPropertyName("parameters")]  public List<ParameterResponse>? Parameters { get; init; }
    }

    private sealed class ParameterResponse
    {
        [JsonPropertyName("name")]        public string Name { get; init; } = "";
        [JsonPropertyName("type")]        public string Type { get; init; } = "";
        [JsonPropertyName("description")] public string Description { get; init; } = "";
        [JsonPropertyName("isRequired")]  public bool IsRequired { get; init; }
    }
}
