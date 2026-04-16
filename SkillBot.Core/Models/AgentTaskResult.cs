namespace SkillBot.Core.Models;

/// <summary>
/// Result of a task execution by an agent.
/// </summary>
public record AgentTaskResult
{
    public required string TaskId { get; init; }
    public required string AgentId { get; init; }
    public required string Result { get; init; }
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public List<string>? ToolsUsed { get; init; }
}