using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Cache.Models;
using SkillBot.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkillBot.Infrastructure.Cache;

/// <summary>
/// Decorator that adds caching to an IChatCompletionService.
/// </summary>
public class CachedChatCompletionService : IChatCompletionService
{
    private readonly IChatCompletionService _innerService;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyBuilder _keyBuilder;
    private readonly CachingOptions _options;
    private readonly ILogger<CachedChatCompletionService> _logger;

    public IReadOnlyDictionary<string, object?> Attributes => _innerService.Attributes;

    public CachedChatCompletionService(
        IChatCompletionService innerService,
        ICacheService cacheService,
        ICacheKeyBuilder keyBuilder,
        CachingOptions options,
        ILogger<CachedChatCompletionService> logger)
    {
        _innerService = innerService;
        _cacheService = cacheService;
        _keyBuilder = keyBuilder;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _innerService.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        }

        // Generate cache key
        var cacheKey = _keyBuilder.BuildChatCompletionKey(chatHistory, executionSettings, _innerService.Attributes.GetValueOrDefault("ModelId") as string);

        // Try to get from cache
        var cached = await _cacheService.GetAsync<CachedChatResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("LLM cache hit for key: {Key}", cacheKey);
            return new[] { cached.ToChatMessageContent() };
        }

        // Cache miss - call inner service
        _logger.LogDebug("LLM cache miss for key: {Key}", cacheKey);
        var response = await _innerService.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);

        // Cache the response
        if (response.Count > 0)
        {
            var ttl = DetermineTtl(chatHistory);
            var cachedResponse = CachedChatResponse.FromChatMessageContent(response[0]);
            await _cacheService.SetAsync(cacheKey, cachedResponse, ttl, "llm", cancellationToken);
            _logger.LogDebug("Cached LLM response with TTL: {TTL}", ttl);
        }

        return response;
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            await foreach (var chunk in _innerService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        // Generate cache key
        var cacheKey = _keyBuilder.BuildChatCompletionKey(chatHistory, executionSettings, _innerService.Attributes.GetValueOrDefault("ModelId") as string);

        // Try to get from cache
        var cached = await _cacheService.GetAsync<CachedChatResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("LLM cache hit for streaming key: {Key}", cacheKey);
            // Return cached content as a single chunk
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, cached.Content, modelId: cached.ModelId);
            yield break;
        }

        // Cache miss - stream and buffer
        _logger.LogDebug("LLM cache miss for streaming key: {Key}", cacheKey);
        var buffer = new StringBuilder();
        string? modelId = null;
        var metadata = new Dictionary<string, object?>();

        await foreach (var chunk in _innerService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken))
        {
            if (chunk.Content != null)
            {
                buffer.Append(chunk.Content);
            }

            if (chunk.ModelId != null)
            {
                modelId = chunk.ModelId;
            }

            if (chunk.Metadata != null)
            {
                foreach (var kvp in chunk.Metadata)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            yield return chunk;
        }

        // Cache the complete response
        if (buffer.Length > 0)
        {
            var ttl = DetermineTtl(chatHistory);
            var cachedResponse = new CachedChatResponse
            {
                Content = buffer.ToString(),
                ModelId = modelId,
                Metadata = metadata,
                CachedAt = DateTime.UtcNow
            };
            await _cacheService.SetAsync(cacheKey, cachedResponse, ttl, "llm", cancellationToken);
            _logger.LogDebug("Cached streaming LLM response with TTL: {TTL}", ttl);
        }
    }

    private TimeSpan DetermineTtl(ChatHistory chatHistory)
    {
        // Check system message for context hints
        var systemMessage = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content?.ToLowerInvariant() ?? string.Empty;

        if (systemMessage.Contains("routing") || systemMessage.Contains("route") || systemMessage.Contains("classifier"))
        {
            return _options.RoutingCacheTtl;
        }

        if (systemMessage.Contains("agent") || systemMessage.Contains("specialized"))
        {
            return _options.AgentCacheTtl;
        }

        return _options.GeneralCacheTtl;
    }
}
