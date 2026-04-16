namespace SkillBot.Core.Models;

/// <summary>
/// Current status of an agent.
/// </summary>
public record AgentStatus
{
    public required string AgentId { get; init; }
    public required string State { get; init; } // "idle", "busy", "error"
    public string? CurrentTask { get; init; }
    public int TasksCompleted { get; init; }
    public int TasksFailed { get; init; }
    public DateTimeOffset LastActive { get; init; }
}