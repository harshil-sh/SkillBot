using SkillBot.Core.Models;

namespace SkillBot.Core.Services;

public interface IMessagingChannel
{
    string Name { get; }
    bool IsConfigured { get; }

    Task<bool> SendMessageAsync(string userId, string message);
    Task<Message?> ReceiveMessageAsync();
    Task<bool> RegisterUserAsync(string channelUserId, string systemUserId);
    Task<User?> GetUserByChannelIdAsync(string channelUserId);
}

public record Message
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Text { get; init; }
    public required string ChannelName { get; init; }
    public required DateTime ReceivedAt { get; init; }
}
