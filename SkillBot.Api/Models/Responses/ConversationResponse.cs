/// <summary>
/// Conversation info response
/// </summary>
public class ConversationResponse
{
    public required string ConversationId { get; init; }
    public List<MessageInfo> Messages { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastActivityAt { get; init; }
    public int MessageCount { get; init; }
}