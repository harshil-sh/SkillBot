// SkillBot.Core/Interfaces/IExecutionContext.cs
namespace SkillBot.Core.Interfaces;

/// <summary>
/// Provides runtime context and metadata for agent execution.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Unique identifier for this execution session.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// When this session started.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Total number of turns (user messages) processed.
    /// </summary>
    int TurnCount { get; }

    /// <summary>
    /// Total number of tool/plugin calls made.
    /// </summary>
    int ToolCallCount { get; }

    /// <summary>
    /// Custom metadata dictionary for extensibility.
    /// </summary>
    IDictionary<string, object> Metadata { get; }
}
