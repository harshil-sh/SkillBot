// SkillBot.Core/Interfaces/IAgentEngine.cs
using SkillBot.Core.Models;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Core orchestration engine responsible for the agent execution loop.
/// Handles communication between user, LLM, and plugins.
/// </summary>
public interface IAgentEngine
{
    /// <summary>
    /// Execute a single turn of the agent loop with the given user message.
    /// </summary>
    /// <param name="message">User input message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent's response including any tool calls made</returns>
    Task<AgentResponse> ExecuteAsync(
        string message, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a streaming turn where responses are yielded incrementally.
    /// </summary>
    /// <param name="message">User input message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async stream of response chunks</returns>
    IAsyncEnumerable<string> ExecuteStreamingAsync(
        string message, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset the conversation history and start fresh.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current execution context with metadata.
    /// </summary>
    IExecutionContext Context { get; }
}
