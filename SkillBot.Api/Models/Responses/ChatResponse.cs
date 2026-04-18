namespace SkillBot.Api.Models.Responses;

/// <summary>
/// Response model for single-agent chat
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The agent's response message
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// Conversation ID for continuing the conversation
    /// </summary>
    public required string ConversationId { get; init; }
    
    /// <summary>
    /// Tools/plugins that were called during this turn
    /// </summary>
    public List<ToolCallInfo> ToolCalls { get; init; } = new();
    
    /// <summary>
    /// Time taken to process the request
    /// </summary>
    public double ExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Number of tokens used (if available)
    /// </summary>
    public int? TokensUsed { get; init; }
    
    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}