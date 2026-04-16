namespace SkillBot.Core.Models;

/// <summary>
/// Decision about how to route a task to agents.
/// </summary>
public record TaskRoutingDecision
{
    public required string Strategy { get; init; } // "single", "parallel", "sequential"
    public List<string> SelectedAgentIds { get; init; } = new();
    public string? Reasoning { get; init; }
    public Dictionary<string, object>? RoutingMetadata { get; init; }
}