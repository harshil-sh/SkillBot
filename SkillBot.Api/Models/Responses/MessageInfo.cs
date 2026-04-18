/// <summary>
/// Message information
/// </summary>
public class MessageInfo
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}