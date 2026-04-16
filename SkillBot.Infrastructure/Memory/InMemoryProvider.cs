// SkillBot.Infrastructure/Memory/InMemoryProvider.cs
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Memory;

/// <summary>
/// In-memory implementation of chat history storage.
/// Thread-safe and suitable for single-session use.
/// </summary>
public class InMemoryProvider : IMemoryProvider
{
    private readonly ConcurrentBag<AgentMessage> _messages;
    private readonly ILogger<InMemoryProvider> _logger;

    public InMemoryProvider(ILogger<InMemoryProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messages = new ConcurrentBag<AgentMessage>();
    }

    public Task AddMessageAsync(
        AgentMessage message, 
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _messages.Add(message);
        
        _logger.LogDebug(
            "Added {Role} message to memory. Total messages: {Count}",
            message.Role,
            _messages.Count);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        int? count = null, 
        CancellationToken cancellationToken = default)
    {
        var messages = _messages
            .OrderBy(m => m.Timestamp)
            .AsEnumerable();

        if (count.HasValue && count.Value > 0)
        {
            messages = messages.TakeLast(count.Value);
        }

        IReadOnlyList<AgentMessage> result = messages.ToList();
        
        _logger.LogDebug("Retrieved {Count} messages from history", result.Count);
        
        return Task.FromResult(result);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _messages.Clear();
        _logger.LogInformation("Cleared all messages from memory");
        return Task.CompletedTask;
    }

    public Task<int> GetMessageCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_messages.Count);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // No-op for in-memory provider
        // Future SQLite implementation would persist to disk here
        return Task.CompletedTask;
    }
}
