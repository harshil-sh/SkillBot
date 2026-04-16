using SkillBot.Core.Models;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Orchestrates multiple specialized agents to work together on complex tasks.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Register a specialized agent with the orchestrator.
    /// </summary>
    void RegisterAgent(ISpecializedAgent agent);

    /// <summary>
    /// Unregister an agent.
    /// </summary>
    bool UnregisterAgent(string agentId);

    /// <summary>
    /// Get all registered agents.
    /// </summary>
    IReadOnlyList<ISpecializedAgent> GetAgents();

    /// <summary>
    /// Execute a task by delegating to the appropriate agent(s).
    /// </summary>
    Task<OrchestratedResponse> ExecuteTaskAsync(
        string userRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a complex multi-step workflow involving multiple agents.
    /// </summary>
    Task<OrchestratedResponse> ExecuteWorkflowAsync(
        AgentWorkflow workflow,
        CancellationToken cancellationToken = default);
}