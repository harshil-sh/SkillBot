// SkillBot.Core/Interfaces/IMemoryProvider.cs
using SkillBot.Core.Models;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Abstracts chat history persistence and retrieval.
/// Designed for future SQLite/database implementations.
/// </summary>
public interface IMemoryProvider
{
    /// <summary>
    /// Add a message to the conversation history.
    /// </summary>
    /// <param name="message">Message to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddMessageAsync(
        AgentMessage message, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve conversation history, optionally filtered.
    /// </summary>
    /// <param name="count">Maximum number of messages to retrieve (null = all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Messages in chronological order</returns>
    Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        int? count = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all conversation history.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total message count in history.
    /// </summary>
    Task<int> GetMessageCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Save the current state to persistent storage (if applicable).
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
