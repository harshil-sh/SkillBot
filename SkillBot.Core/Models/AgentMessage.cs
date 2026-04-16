namespace SkillBot.Core.Models;

/// <summary>
/// Represents a single message in the conversation.
/// </summary>
public record AgentMessage
{
    public required string Role { get; init; } // "user", "assistant", "system"
    public required string Content { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object>? Metadata { get; init; }
}