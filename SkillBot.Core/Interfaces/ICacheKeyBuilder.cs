using System.Collections.Generic;

namespace SkillBot.Core.Interfaces;

/// <summary>
/// Builds deterministic cache keys for different types of cacheable operations.
/// </summary>
public interface ICacheKeyBuilder
{
    /// <summary>
    /// Builds a cache key for a chat completion request.
    /// </summary>
    string BuildChatCompletionKey(object chatHistory, object? settings, string? modelId);

    /// <summary>
    /// Builds a cache key for task routing.
    /// </summary>
    string BuildRoutingKey(string userMessage);

    /// <summary>
    /// Builds a cache key for a web search.
    /// </summary>
    string BuildSearchKey(string searchType, string query, int count);

    /// <summary>
    /// Builds a cache key for agent execution.
    /// </summary>
    string BuildAgentExecutionKey(string agentType, string input, Dictionary<string, object>? parameters);
}
