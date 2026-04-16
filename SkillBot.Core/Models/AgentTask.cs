namespace SkillBot.Core.Models;

/// <summary>
/// Represents a task to be executed by an agent.
/// </summary>
public record AgentTask
{
    public required string TaskId { get; init; }
    public required string Description { get; init; }
    public required string UserRequest { get; init; }
    public Dictionary<string, object>? Context { get; init; }
    public int Priority { get; init; } = 0;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ParentTaskId { get; init; }
}