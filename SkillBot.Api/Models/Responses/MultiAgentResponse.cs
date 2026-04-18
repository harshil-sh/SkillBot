/// <summary>
/// Response model for multi-agent chat
/// </summary>
public class MultiAgentResponse
{
    /// <summary>
    /// The synthesized final response
    /// </summary>
    public required string FinalResponse { get; init; }
    
    /// <summary>
    /// Conversation ID
    /// </summary>
    public required string ConversationId { get; init; }
    
    /// <summary>
    /// Agents that were involved in processing this request
    /// </summary>
    public List<AgentExecutionInfo> AgentsUsed { get; init; } = new();
    
    /// <summary>
    /// Execution strategy used (single, parallel, sequential)
    /// </summary>
    public required string Strategy { get; init; }
    
    /// <summary>
    /// Total time taken
    /// </summary>
    public double TotalExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}