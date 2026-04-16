namespace SkillBot.Core.Models;

/// <summary>
/// A single step in an agent workflow.
/// </summary>
public record WorkflowStep
{
    public required string StepId { get; init; }
    public required string Description { get; init; }
    public string? RequiredAgentId { get; init; }
    public List<string>? DependsOn { get; init; }
    public Dictionary<string, object>? StepContext { get; init; }
}