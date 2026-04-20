using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Repositories;

namespace SkillBot.Infrastructure.Channels;

public class TelegramChannel : BaseMessagingChannel
{
    private readonly string? _botToken;
    private TelegramBotClient? _botClient;

    public override string Name => "telegram";

    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_botToken);

    public TelegramChannel(
        IConfiguration configuration,
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger<TelegramChannel> logger)
        : base(channelUserRepository, userRepository, logger)
    {
        _botToken = configuration["Channels:Telegram:BotToken"];

        if (IsConfigured)
            _botClient = new TelegramBotClient(_botToken!);
    }

    public override async Task<bool> SendMessageAsync(string userId, string message)
    {
        if (!IsConfigured || _botClient is null)
        {
            Logger.LogWarning("Telegram channel is not configured; cannot send message to {UserId}", userId);
            return false;
        }

        if (!long.TryParse(userId, out var chatId))
        {
            Logger.LogWarning("Invalid Telegram chat ID: {UserId}", userId);
            return false;
        }

        try
        {
            await _botClient.SendMessage(chatId, message);

            Logger.LogDebug("Sent Telegram message to chat {ChatId}", chatId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send Telegram message to chat {ChatId}", chatId);
            return false;
        }
    }

    /// <summary>
    /// Not used — Telegram delivers messages via webhook push.
    /// </summary>
    public override Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null);
}
