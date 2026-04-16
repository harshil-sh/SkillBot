namespace SkillBot.Core.Models;

/// <summary>
/// Response from the orchestrator after coordinating multiple agents.
/// </summary>
public record OrchestratedResponse
{
    public required string FinalResponse { get; init; }
    public List<AgentTaskResult> AgentResults { get; init; } = new();
    public TimeSpan TotalExecutionTime { get; init; }
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}