using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;

namespace SkillBot.Infrastructure.Cache.Models;

/// <summary>
/// Cached representation of a chat response.
/// </summary>
public class CachedChatResponse
{
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public string? ModelId { get; set; }
    public DateTime CachedAt { get; set; }

    public static CachedChatResponse FromChatMessageContent(ChatMessageContent message)
    {
        return new CachedChatResponse
        {
            Content = message.Content ?? string.Empty,
            Metadata = new Dictionary<string, object?>(message.Metadata ?? new Dictionary<string, object?>()),
            ModelId = message.ModelId,
            CachedAt = DateTime.UtcNow
        };
    }

    public ChatMessageContent ToChatMessageContent()
    {
        return new ChatMessageContent(
            role: AuthorRole.Assistant,
            content: Content,
            modelId: ModelId,
            metadata: Metadata
        );
    }
}
