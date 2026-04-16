// SkillBot.Infrastructure/Engine/SemanticKernelEngine.cs
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SkillBot.Core.Exceptions;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;

namespace SkillBot.Infrastructure.Engine;

/// <summary>
/// Semantic Kernel implementation of the agent execution engine.
/// Orchestrates the message loop between user, LLM, and plugins.
/// </summary>
public class SemanticKernelEngine : IAgentEngine
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly IMemoryProvider _memoryProvider;
    private readonly IPluginProvider _pluginProvider;
    private readonly ILogger<SemanticKernelEngine> _logger;
    private readonly ExecutionContext _context;
    private readonly ChatHistory _chatHistory;
    private readonly OpenAIPromptExecutionSettings _executionSettings;

    public IExecutionContext Context => _context;

    public SemanticKernelEngine(
        Kernel kernel,
        IChatCompletionService chatService,
        IMemoryProvider memoryProvider,
        IPluginProvider pluginProvider,
        ILogger<SemanticKernelEngine> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _memoryProvider = memoryProvider ?? throw new ArgumentNullException(nameof(memoryProvider));
        _pluginProvider = pluginProvider ?? throw new ArgumentNullException(nameof(pluginProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _context = new ExecutionContext();
        _chatHistory = new ChatHistory();
        
        // Configure automatic tool calling
        _executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7,
            MaxTokens = 2000
        };

        InitializeSystemPrompt();
    }

    private void InitializeSystemPrompt()
    {
        var systemPrompt = """
            You are SkillBot, a helpful AI assistant with access to various tools and plugins.
            When users ask you to perform tasks, use the available tools to help them.
            Always explain what you're doing and provide clear, concise responses.
            """;
        
        _chatHistory.AddSystemMessage(systemPrompt);
    }

    public async Task<AgentResponse> ExecuteAsync(
        string message, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        var stopwatch = Stopwatch.StartNew();
        var toolCalls = new List<ToolCall>();

        try
        {
            _logger.LogInformation("Processing user message: {Message}", message);

            // Add user message to history
            _chatHistory.AddUserMessage(message);
            await _memoryProvider.AddMessageAsync(
                new AgentMessage { Role = "user", Content = message },
                cancellationToken);

            _context.IncrementTurn();

            // Get response from LLM with automatic tool invocation
            var result = await _chatService.GetChatMessageContentAsync(
                _chatHistory,
                _executionSettings,
                _kernel,
                cancellationToken);

            var responseContent = result.Content ?? string.Empty;

            // Add assistant response to history
            _chatHistory.AddAssistantMessage(responseContent);
            await _memoryProvider.AddMessageAsync(
                new AgentMessage { Role = "assistant", Content = responseContent },
                cancellationToken);

            // Extract tool call information from metadata if available
            if (result.Metadata?.ContainsKey("ToolCalls") == true)
            {
                // Parse tool calls from metadata
                toolCalls = ExtractToolCalls(result.Metadata);
                _context.IncrementToolCalls(toolCalls.Count);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Response generated in {ElapsedMs}ms with {ToolCallCount} tool calls",
                stopwatch.ElapsedMilliseconds,
                toolCalls.Count);

            return new AgentResponse
            {
                Content = responseContent,
                ToolCalls = toolCalls,
                ExecutionTime = stopwatch.Elapsed,
                TokensUsed = 0, // Semantic Kernel doesn't always expose token counts
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing agent turn");

            return new AgentResponse
            {
                Content = "I encountered an error processing your request.",
                ExecutionTime = stopwatch.Elapsed,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async IAsyncEnumerable<string> ExecuteStreamingAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        _logger.LogInformation("Processing streaming message: {Message}", message);

        // Add user message to history
        _chatHistory.AddUserMessage(message);
        await _memoryProvider.AddMessageAsync(
            new AgentMessage { Role = "user", Content = message },
            cancellationToken);

        _context.IncrementTurn();

        // Stream the response
        var fullResponse = string.Empty;
        await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(
            _chatHistory,
            _executionSettings,
            _kernel,
            cancellationToken))
        {
            var content = chunk.Content ?? string.Empty;
            fullResponse += content;
            yield return content;
        }

        // Add complete response to history
        _chatHistory.AddAssistantMessage(fullResponse);
        await _memoryProvider.AddMessageAsync(
            new AgentMessage { Role = "assistant", Content = fullResponse },
            cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetting agent session");
        
        _chatHistory.Clear();
        InitializeSystemPrompt();
        
        await _memoryProvider.ClearAsync(cancellationToken);
        
        _context.Reset();
    }

    private List<ToolCall> ExtractToolCalls(IReadOnlyDictionary<string, object?> metadata)
    {
        // This is a simplified extraction - in practice, you'd parse the actual
        // tool call metadata from Semantic Kernel's response
        var toolCalls = new List<ToolCall>();
        
        // Semantic Kernel provides tool call information in metadata
        // The exact structure depends on the SK version
        
        return toolCalls;
    }

    private class ExecutionContext : IExecutionContext
    {
        public string SessionId { get; } = Guid.NewGuid().ToString();
        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
        public int TurnCount { get; private set; }
        public int ToolCallCount { get; private set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public void IncrementTurn() => TurnCount++;
        public void IncrementToolCalls(int count) => ToolCallCount += count;
        public void Reset()
        {
            TurnCount = 0;
            ToolCallCount = 0;
            Metadata.Clear();
        }
    }
}
