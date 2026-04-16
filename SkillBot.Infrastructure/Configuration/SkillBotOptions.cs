// SkillBot.Infrastructure/Configuration/SkillBotOptions.cs
namespace SkillBot.Infrastructure.Configuration;

/// <summary>
/// Configuration options for SkillBot.
/// </summary>
public class SkillBotOptions
{
    public const string SectionName = "SkillBot";

    /// <summary>
    /// OpenAI API Key (or Azure OpenAI key)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use (e.g., "gpt-4", "gpt-3.5-turbo")
    /// </summary>
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Optional: Azure OpenAI endpoint (leave empty for OpenAI)
    /// </summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>
    /// Optional: Azure OpenAI deployment name
    /// </summary>
    public string? AzureDeploymentName { get; set; }

    /// <summary>
    /// Maximum number of messages to keep in memory
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 100;

    /// <summary>
    /// Enable verbose logging
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Paths to assemblies containing plugins
    /// </summary>
    public List<string> PluginAssemblyPaths { get; set; } = new();

    /// <summary>
    /// Memory provider type: "InMemory" or "SQLite"
    /// </summary>
    public string MemoryProvider { get; set; } = "InMemory";

    /// <summary>
    /// SQLite database file path (used when MemoryProvider = "SQLite")
    /// </summary>
    public string SqliteDatabasePath { get; set; } = "skillbot.db";
}
