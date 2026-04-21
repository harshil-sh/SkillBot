using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace SkillBot.Infrastructure.Channels;

/// <summary>
/// Centralises all Telegram configuration lookups.
/// </summary>
public static class TelegramConfigHelper
{
    private const string EnabledKey    = "Channels:Telegram:Enabled";
    private const string BotTokenKey   = "Channels:Telegram:BotToken";
    private const string WebhookUrlKey = "Channels:Telegram:WebhookUrl";
    private const string BotUsernameKey = "Channels:Telegram:BotUsername";

    public static bool IsEnabled(IConfiguration config) =>
        config.GetValue<bool>(EnabledKey);

    public static string? GetBotToken(IConfiguration config) =>
        config[BotTokenKey];

    public static string? GetWebhookUrl(IConfiguration config) =>
        config[WebhookUrlKey];

    public static string? GetBotUsername(IConfiguration config) =>
        config[BotUsernameKey];

    /// <summary>
    /// Registers the webhook URL with the Telegram Bot API.
    /// </summary>
    public static async Task RegisterWebhookAsync(string botToken, string webhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);

        var client = new TelegramBotClient(botToken);
        await client.SetWebhook(webhookUrl);
    }
}
