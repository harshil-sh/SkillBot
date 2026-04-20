namespace SkillBot.Core.Models;

/// <summary>
/// Configuration for a messaging channel (e.g. Telegram, WhatsApp).
/// Channel-specific values such as tokens and URLs are stored in <see cref="Settings"/>.
/// </summary>
/// <example>
/// Telegram: Settings["BotToken"], Settings["WebhookUrl"]
/// WhatsApp: Settings["AccountSid"], Settings["AuthToken"]
/// </example>
public record ChannelConfiguration
{
    public required string Name { get; init; }
    public required bool Enabled { get; init; }
    public Dictionary<string, string> Settings { get; init; } = [];
}
