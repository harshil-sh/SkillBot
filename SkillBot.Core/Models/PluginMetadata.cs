namespace SkillBot.Core.Models;

/// <summary>
/// Metadata describing a registered plugin.
/// </summary>
public record PluginMetadata
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<FunctionMetadata> Functions { get; init; } = new();
    public Type PluginType { get; init; } = typeof(object);
}