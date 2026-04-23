using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using SkillBot.Core.Services;

namespace SkillBot.Infrastructure.Channels;

public class TelegramChannel : BaseMessagingChannel
{
    private const int MaxTelegramMessageLength = 4000;

    private readonly string? _botToken;
    private readonly IServiceScopeFactory _scopeFactory;
    private TelegramBotClient? _botClient;

    public override string Name => "telegram";

    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_botToken);

    public TelegramChannel(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramChannel> logger)
        : base(scopeFactory, logger)
    {
        _botToken = TelegramConfigHelper.GetBotToken(configuration);
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

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
            await _botClient.SendChatAction(chatId, ChatAction.Typing);

            foreach (var chunk in SplitMessage(message))
            {
                await _botClient.SendMessage(chatId, chunk);
            }

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
    /// Handles Telegram slash commands.
    /// Returns true if the command was handled.
    /// </summary>
    public async Task<bool> HandleCommandAsync(string chatId, string command, string? arguments)
    {
        if (!IsConfigured || _botClient is null)
            return false;

        if (!long.TryParse(chatId, out var id))
            return false;

        try
        {
            switch (command.ToLowerInvariant())
            {
                case "/start":
                    await _botClient.SendMessage(id,
                        "👋 Welcome to *SkillBot*!\n\n" +
                        "I'm your AI assistant powered by OpenAI, Claude, or Gemini.\n\n" +
                        "Type any message to start chatting, or use /help for a list of commands.",
                        parseMode: ParseMode.Markdown);
                    break;

                case "/help":
                    await _botClient.SendMessage(id,
                        "*SkillBot Commands*\n\n" +
                        "/start — Welcome message\n" +
                        "/help — Show this help\n" +
                        "/settings — View your current settings\n" +
                        "/setkey <provider> <key> — Set API key (openai/claude/gemini/serpapi)\n" +
                        "/setprovider <provider> — Change LLM provider\n" +
                        "/search <query> — Web search\n" +
                        "/multi <message> — Multi-agent response\n\n" +
                        "Just type a message to chat!",
                        parseMode: ParseMode.Markdown);
                    break;

                case "/settings":
                {
                    var user = await GetUserByChannelIdAsync(chatId);
                    if (user is null)
                    {
                        await _botClient.SendMessage(id, "⚠️ Could not find your account.");
                        break;
                    }
                    using var scope = _scopeFactory.CreateScope();
                    var settingsSvc = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();
                    var settings = await settingsSvc.GetSettingsAsync(user.Id);
                    await _botClient.SendMessage(id,
                        $"*Your Settings*\n\n" +
                        $"Provider: `{settings.PreferredProvider}`\n" +
                        $"OpenAI key: {(settings.HasOpenAiKey ? "✅ Set" : "❌ Not set")}\n" +
                        $"Claude key: {(settings.HasClaudeKey ? "✅ Set" : "❌ Not set")}\n" +
                        $"Gemini key: {(settings.HasGeminiKey ? "✅ Set" : "❌ Not set")}\n" +
                        $"SerpAPI key: {(settings.HasSerpApiKey ? "✅ Set" : "❌ Not set")}",
                        parseMode: ParseMode.Markdown);
                    break;
                }

                case "/setkey":
                {
                    var parts = (arguments ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        await _botClient.SendMessage(id, "Usage: /setkey <provider> <key>\nProviders: openai, claude, gemini, serpapi");
                        break;
                    }
                    var user = await GetUserByChannelIdAsync(chatId);
                    if (user is null)
                    {
                        await _botClient.SendMessage(id, "⚠️ Could not find your account.");
                        break;
                    }
                    using var scope = _scopeFactory.CreateScope();
                    var settingsSvc = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();
                    await settingsSvc.UpdateApiKeyAsync(user.Id, parts[0], parts[1]);
                    await _botClient.SendMessage(id, $"✅ {parts[0]} API key updated.");
                    break;
                }

                case "/setprovider":
                {
                    var provider = (arguments ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(provider))
                    {
                        await _botClient.SendMessage(id, "Usage: /setprovider <provider>\nProviders: openai, claude, gemini");
                        break;
                    }
                    var user = await GetUserByChannelIdAsync(chatId);
                    if (user is null)
                    {
                        await _botClient.SendMessage(id, "⚠️ Could not find your account.");
                        break;
                    }
                    using var scope = _scopeFactory.CreateScope();
                    var settingsSvc = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();
                    await settingsSvc.UpdateProviderAsync(user.Id, provider);
                    await _botClient.SendMessage(id, $"✅ Provider changed to `{provider}`.", parseMode: ParseMode.Markdown);
                    break;
                }

                case "/search":
                    // Handled by the webhook handler which routes to agent engine
                    return false;

                case "/multi":
                    // Handled by the webhook handler
                    return false;

                default:
                    await _botClient.SendMessage(id, "❓ Unknown command. Type /help for a list of commands.");
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling Telegram command {Command} for chat {ChatId}", command, chatId);
            return false;
        }
    }

    /// <summary>
    /// Not used — Telegram delivers messages via webhook push.
    /// </summary>
    public override Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IEnumerable<string> SplitMessage(string message)
    {
        if (message.Length <= MaxTelegramMessageLength)
        {
            yield return message;
            yield break;
        }

        for (int i = 0; i < message.Length; i += MaxTelegramMessageLength)
        {
            yield return message.Substring(i, Math.Min(MaxTelegramMessageLength, message.Length - i));
        }
    }
}

