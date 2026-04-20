namespace SkillBot.Core.Models;

/// <summary>
/// Maps a channel-specific user identity to an internal system user account.
/// </summary>
public record ChannelUser
{
    public required string Id { get; init; }

    /// <summary>FK to <see cref="User.Id"/>.</summary>
    public required string SystemUserId { get; init; }

    /// <summary>Channel name, e.g. "telegram" or "whatsapp".</summary>
    public required string ChannelName { get; init; }

    /// <summary>Channel-native identifier, e.g. a Telegram chat ID or WhatsApp phone number.</summary>
    public required string ChannelUserId { get; init; }

    public required DateTime RegisteredAt { get; init; }
}
