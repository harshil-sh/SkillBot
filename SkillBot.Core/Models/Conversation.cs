namespace SkillBot.Core.Models;

public record Conversation
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Message { get; init; }
    public required string Response { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int TokensUsed { get; init; }
    public string? ConversationId { get; init; }
}
