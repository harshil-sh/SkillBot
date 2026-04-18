/// <summary>
/// Information about an agent's execution
/// </summary>
public class AgentExecutionInfo
{
    public required string AgentId { get; init; }
    public required string AgentName { get; init; }
    public required string Result { get; init; }
    public double ExecutionTimeMs { get; init; }
    public bool Success { get; init; }
}