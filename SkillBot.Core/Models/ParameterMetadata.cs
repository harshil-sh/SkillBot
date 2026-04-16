namespace SkillBot.Core.Models;

/// <summary>
/// Metadata for a function parameter.
/// </summary>
public record ParameterMetadata
{
    public required string Name { get; init; }
    public required Type Type { get; init; }
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public object? DefaultValue { get; init; }
}