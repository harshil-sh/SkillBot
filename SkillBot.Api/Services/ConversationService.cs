using Microsoft.Extensions.Caching.Memory;
using SkillBot.Api.Models.Responses;

namespace SkillBot.Api.Services;

/// <summary>
/// Manages conversation sessions and history
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConversationService> _logger;
    private readonly TimeSpan _conversationTtl = TimeSpan.FromHours(24);

    public ConversationService(
        IMemoryCache cache,
        ILogger<ConversationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<string> CreateConversationAsync()
    {
        var conversationId = $"conv_{Guid.NewGuid():N}";
        
        var conversation = new ConversationData
        {
            ConversationId = conversationId,
            Messages = new List<MessageData>(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_conversationTtl)
            .SetSize(1); // Each conversation counts as 1 unit

        _cache.Set(conversationId, conversation, cacheOptions);
        
        _logger.LogInformation("Created conversation {ConversationId}", conversationId);
        
        return Task.FromResult(conversationId);
    }

    public Task<ConversationResponse?> GetConversationAsync(string conversationId)
    {
        if (_cache.TryGetValue<ConversationData>(conversationId, out var conversation)
            && conversation is not null)
        {
            var response = new ConversationResponse
            {
                ConversationId = conversation.ConversationId,
                CreatedAt = conversation.CreatedAt,
                LastActivityAt = conversation.LastActivityAt,
                MessageCount = conversation.Messages.Count,
                Messages = conversation.Messages.Select(m => new MessageInfo
                {
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.Timestamp
                }).ToList()
            };

            return Task.FromResult<ConversationResponse?>(response);
        }

        return Task.FromResult<ConversationResponse?>(null);
    }

    public Task SaveMessageAsync(string conversationId, string role, string content)
    {
        if (_cache.TryGetValue<ConversationData>(conversationId, out var conversation)
            && conversation is not null)
        {
            var message = new MessageData
            {
                Role = role,
                Content = content,
                Timestamp = DateTimeOffset.UtcNow
            };

            conversation.Messages.Add(message);
            conversation.LastActivityAt = DateTimeOffset.UtcNow;

            // Update cache with extended TTL
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_conversationTtl)
                .SetSize(1); // Each conversation counts as 1 unit

            _cache.Set(conversationId, conversation, cacheOptions);
            
            _logger.LogDebug(
                "Saved {Role} message to conversation {ConversationId}",
                role,
                conversationId);
        }
        else
        {
            _logger.LogWarning(
                "Attempted to save message to non-existent conversation {ConversationId}",
                conversationId);
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteConversationAsync(string conversationId)
    {
        if (_cache.TryGetValue<ConversationData>(conversationId, out _))
        {
            _cache.Remove(conversationId);
            _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    // Internal data models
    private class ConversationData
    {
        public required string ConversationId { get; init; }
        public List<MessageData> Messages { get; init; } = new();
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset LastActivityAt { get; set; }
        
        // TODO: Add UserId here when implementing OAuth (Phase 2)
        // public string? UserId { get; init; }
    }

    private class MessageData
    {
        public required string Role { get; init; }
        public required string Content { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}
