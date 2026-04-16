using SkillBot.Core.Models;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Routes tasks to the most appropriate agent(s).
/// </summary>
public interface ITaskRouter
{
    /// <summary>
    /// Analyze a user request and determine which agent(s) should handle it.
    /// </summary>
    Task<TaskRoutingDecision> RouteTaskAsync(
        string userRequest,
        IReadOnlyList<ISpecializedAgent> availableAgents,
        CancellationToken cancellationToken = default);
}