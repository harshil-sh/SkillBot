namespace SkillBot.Core.Models;

/// <summary>
/// The agent's response to a user message, including tool execution details.
/// </summary>
public record AgentResponse
{
    public required string Content { get; init; }
    public List<ToolCall> ToolCalls { get; init; } = new();
    public TimeSpan ExecutionTime { get; init; }
    public int TokensUsed { get; init; }
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
}