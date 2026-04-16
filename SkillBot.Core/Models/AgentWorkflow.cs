namespace SkillBot.Core.Models;

/// <summary>
/// Defines a multi-step workflow involving multiple agents.
/// </summary>
public record AgentWorkflow
{
    public required string WorkflowId { get; init; }
    public required string Description { get; init; }
    public List<WorkflowStep> Steps { get; init; } = new();
    public Dictionary<string, object>? InitialContext { get; init; }
}