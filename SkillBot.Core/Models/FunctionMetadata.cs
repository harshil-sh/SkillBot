namespace SkillBot.Core.Models;

/// <summary>
/// Metadata for a single function within a plugin.
/// </summary>
public record FunctionMetadata
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<ParameterMetadata> Parameters { get; init; } = new();
}