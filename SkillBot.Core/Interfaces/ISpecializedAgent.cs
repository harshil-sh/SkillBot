using SkillBot.Core.Models;
namespace SkillBot.Core.Interfaces;

/// <summary>
/// Represents a specialized agent that handles specific types of tasks.
/// </summary>
public interface ISpecializedAgent
{
    /// <summary>
    /// Unique identifier for this agent.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Display name of the agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of the agent's capabilities.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Specialization areas (e.g., "coding", "research", "analysis")
    /// </summary>
    IReadOnlyList<string> Specializations { get; }

    /// <summary>
    /// Determine if this agent can handle the given task.
    /// </summary>
    Task<bool> CanHandleAsync(AgentTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a task assigned to this agent.
    /// </summary>
    Task<AgentTaskResult> ExecuteAsync(AgentTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current status of this agent.
    /// </summary>
    AgentStatus GetStatus();
}