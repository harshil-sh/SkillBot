using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkillBot.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SkillBot.Infrastructure.Cache;

/// <summary>
/// Builds deterministic cache keys using SHA256 hashing.
/// </summary>
public class CacheKeyBuilder : ICacheKeyBuilder
{
    public string BuildChatCompletionKey(object chatHistory, object? settings, string? modelId)
    {
        var components = new StringBuilder();

        // Add model ID
        components.Append($"model:{modelId ?? "default"}|");

        // Add chat history
        if (chatHistory is ChatHistory history)
        {
            components.Append("history:");
            foreach (var message in history)
            {
                components.Append($"{message.Role}:{message.Content}|");
            }
        }

        // Add settings
        if (settings is PromptExecutionSettings execSettings)
        {
            // Serialize settings to ensure consistent hashing
            var settingsJson = JsonSerializer.Serialize(execSettings);
            components.Append($"settings:{settingsJson}|");
        }

        var hash = ComputeSha256Hash(components.ToString());
        return $"llm:{hash}";
    }

    public string BuildRoutingKey(string userMessage)
    {
        var hash = ComputeSha256Hash($"routing:{userMessage}");
        return $"routing:{hash}";
    }

    public string BuildSearchKey(string searchType, string query, int count)
    {
        var hash = ComputeSha256Hash($"{searchType}:{query}:{count}");
        return $"search:{searchType}:{hash}";
    }

    public string BuildAgentExecutionKey(string agentType, string input, Dictionary<string, object>? parameters)
    {
        var components = new StringBuilder();
        components.Append($"agent:{agentType}|input:{input}|");

        if (parameters != null && parameters.Count > 0)
        {
            var sortedParams = parameters.OrderBy(p => p.Key);
            foreach (var param in sortedParams)
            {
                components.Append($"{param.Key}:{JsonSerializer.Serialize(param.Value)}|");
            }
        }

        var hash = ComputeSha256Hash(components.ToString());
        return $"agent:{agentType}:{hash}";
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
