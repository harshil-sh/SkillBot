namespace SkillBot.Api.Models.Requests;

/// <summary>
/// Request model for single-agent chat
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// User's message to the agent
    /// </summary>
    /// <example>What's the weather like today?</example>
    public required string Message { get; init; }
    
    /// <summary>
    /// Optional conversation ID to continue existing conversation
    /// </summary>
    /// <example>conv_123abc</example>
    public string? ConversationId { get; init; }
    
    /// <summary>
    /// Enable streaming response (not implemented yet)
    /// </summary>
    public bool Stream { get; init; } = false;
}

/// <summary>
/// Request model for multi-agent chat
/// </summary>
public class MultiAgentRequest
{
    /// <summary>
    /// User's task or query for the multi-agent system
    /// </summary>
    /// <example>Research Python vs JavaScript and write a comparison</example>
    public required string Task { get; init; }
    
    /// <summary>
    /// Optional conversation ID
    /// </summary>
    public string? ConversationId { get; init; }
    
    /// <summary>
    /// Optional: Specify which agents to use (null = auto-routing)
    /// </summary>
    /// <example>["research-agent", "writing-agent"]</example>
    public List<string>? PreferredAgents { get; init; }
}
