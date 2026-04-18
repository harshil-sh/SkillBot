/// <summary>
/// Agent status response
/// </summary>
public class AgentStatusResponse
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<string> Specializations { get; init; } = new();
    public required string Status { get; init; }
}