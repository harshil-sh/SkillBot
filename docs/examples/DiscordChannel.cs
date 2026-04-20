// ============================================================================
// DiscordChannel.cs — Reference implementation (NOT production-ready)
//
// This file is a skeleton showing how to integrate Discord using Discord.Net,
// the most widely used .NET library for the Discord API.
//
// NuGet packages required:
//   dotnet add package Discord.Net --version 3.x.x
//
// Discord bot setup:
//   1. Go to https://discord.com/developers/applications and create a new application
//   2. Under "Bot", create a bot user and copy the token
//   3. Under "Privileged Gateway Intents", enable:
//        Message Content Intent  — required to read message text
//        Server Members Intent   — optional, needed for user lookups
//   4. Under "OAuth2 > URL Generator", select scopes: bot
//      Add permissions: Send Messages, Read Message History
//   5. Use the generated URL to invite the bot to your server
//
// Architecture note:
//   Discord.Net uses a persistent WebSocket gateway (not webhooks).
//   The bot connects on startup and receives events in real time.
//   This differs from Telegram/Slack/WhatsApp which use HTTP webhooks.
//   Therefore DiscordChannel manages its own background connection rather
//   than relying on a webhook controller.
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using SkillBot.Infrastructure.Repositories;

// TODO: Add <PackageReference Include="Discord.Net" Version="3.x.x" /> to SkillBot.Infrastructure.csproj
// using Discord;
// using Discord.WebSocket;

namespace SkillBot.Infrastructure.Channels;

/// <summary>
/// Discord channel via Discord.Net WebSocket gateway.
/// Unlike HTTP-webhook channels, this maintains a persistent connection to the Discord gateway.
/// Call <see cref="StartAsync"/> once at application startup to connect the bot.
/// </summary>
public class DiscordChannel : BaseMessagingChannel
{
    private readonly string? _botToken;

    // TODO: Uncomment once Discord.Net is added
    // private readonly DiscordSocketClient _discordClient;

    // Raised when an inbound message has been parsed into a SkillBot Message.
    // Wire this event in WebhookHandlerService or a hosted service to process messages.
    public event Func<Message, Task>? MessageReceived;

    public override string Name => "discord";

    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_botToken);

    public DiscordChannel(
        IConfiguration configuration,
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger<DiscordChannel> logger)
        : base(channelUserRepository, userRepository, logger)
    {
        _botToken = configuration["Channels:Discord:BotToken"];

        // TODO: Initialise Discord client with required intents
        // _discordClient = new DiscordSocketClient(new DiscordSocketConfig
        // {
        //     GatewayIntents = GatewayIntents.DirectMessages
        //                    | GatewayIntents.GuildMessages
        //                    | GatewayIntents.MessageContent
        // });
        //
        // _discordClient.MessageReceived += HandleDiscordMessageAsync;
        // _discordClient.Log += log =>
        // {
        //     Logger.LogInformation("[Discord] {Message}", log.Message);
        //     return Task.CompletedTask;
        // };
    }

    /// <summary>
    /// Connects the Discord bot to the gateway. Call once at application startup.
    /// </summary>
    public async Task StartAsync()
    {
        if (!IsConfigured)
        {
            Logger.LogWarning("Discord channel is not configured; skipping connection");
            return;
        }

        // TODO: Uncomment once Discord.Net is added
        // await _discordClient.LoginAsync(TokenType.Bot, _botToken);
        // await _discordClient.StartAsync();

        Logger.LogInformation("Discord bot started");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disconnects from the Discord gateway. Call during application shutdown.
    /// </summary>
    public async Task StopAsync()
    {
        // TODO: Uncomment once Discord.Net is added
        // await _discordClient.StopAsync();
        // await _discordClient.LogoutAsync();

        Logger.LogInformation("Discord bot stopped");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to a Discord channel or DM.
    /// </summary>
    /// <param name="userId">Discord channel ID (as a string), e.g. "123456789012345678".</param>
    /// <param name="message">Text content to send (supports Discord markdown).</param>
    public override async Task<bool> SendMessageAsync(string userId, string message)
    {
        if (!IsConfigured)
        {
            Logger.LogWarning("Discord channel is not configured; cannot send message to {UserId}", userId);
            return false;
        }

        if (!ulong.TryParse(userId, out var channelId))
        {
            Logger.LogWarning("Invalid Discord channel ID: {UserId}", userId);
            return false;
        }

        try
        {
            // TODO: Uncomment once Discord.Net is added
            // var channel = _discordClient.GetChannel(channelId) as IMessageChannel
            //               ?? await _discordClient.GetChannelAsync(channelId) as IMessageChannel;
            // if (channel is null)
            // {
            //     Logger.LogWarning("Discord channel {ChannelId} not found", channelId);
            //     return false;
            // }
            // await channel.SendMessageAsync(message);

            // TODO: Remove this placeholder log and uncomment the block above
            Logger.LogInformation(
                "[PLACEHOLDER] Would send Discord message to channel {ChannelId}: {Message}", channelId, message);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send Discord message to channel {ChannelId}", channelId);
            return false;
        }
    }

    /// <summary>
    /// Not used — Discord delivers messages via the WebSocket gateway event <see cref="MessageReceived"/>.
    /// </summary>
    public override Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null);

    // ── Private gateway event handler ────────────────────────────────────────

    // TODO: Uncomment once Discord.Net is added
    // private async Task HandleDiscordMessageAsync(SocketMessage socketMessage)
    // {
    //     // Ignore messages from bots (including ourselves) to prevent response loops
    //     if (socketMessage.Author.IsBot)
    //         return;
    //
    //     // Only handle messages in DM channels or if the bot is mentioned in a guild channel
    //     // TODO: Adjust this filter for your use case
    //     if (socketMessage.Channel is not IDMChannel && !socketMessage.MentionedUsers.Any(u => u.Id == _discordClient.CurrentUser.Id))
    //         return;
    //
    //     // Strip the bot mention from the message text, if present
    //     var text = socketMessage.Content
    //         .Replace($"<@{_discordClient.CurrentUser.Id}>", "")
    //         .Replace($"<@!{_discordClient.CurrentUser.Id}>", "")
    //         .Trim();
    //
    //     var message = new Message
    //     {
    //         Id          = socketMessage.Id.ToString(),
    //         UserId      = socketMessage.Channel.Id.ToString(), // reply to the channel, not the user
    //         Text        = text,
    //         ChannelName = Name,
    //         ReceivedAt  = socketMessage.Timestamp.UtcDateTime
    //     };
    //
    //     if (MessageReceived is not null)
    //         await MessageReceived(message);
    // }
}

// ============================================================================
// DiscordBackgroundService.cs — Create in SkillBot.Api/Services/ (or Infrastructure)
// ============================================================================
//
// Discord.Net uses a WebSocket gateway, not webhooks, so you need an IHostedService
// to manage the connection lifecycle rather than a webhook controller.
//
// public class DiscordBackgroundService : BackgroundService
// {
//     private readonly IChannelManager _channelManager;
//     private readonly IWebhookHandlerService _handler;
//     private readonly ILogger<DiscordBackgroundService> _logger;
//
//     public DiscordBackgroundService(
//         IChannelManager channelManager,
//         IWebhookHandlerService handler,
//         ILogger<DiscordBackgroundService> logger)
//     {
//         _channelManager = channelManager;
//         _handler = handler;
//         _logger  = logger;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         if (_channelManager.GetChannel("discord") is not DiscordChannel discord)
//             return;
//
//         // Wire the inbound message event before connecting
//         discord.MessageReceived += _handler.HandleMessageAsync;
//
//         await discord.StartAsync();
//
//         // Keep alive until the application shuts down
//         await Task.Delay(Timeout.Infinite, stoppingToken);
//
//         await discord.StopAsync();
//         discord.MessageReceived -= _handler.HandleMessageAsync;
//     }
// }
// ============================================================================

// ============================================================================
// appsettings.json — Add the following section:
// ============================================================================
//
// "Channels": {
//   "Discord": {
//     "Enabled": false,
//     "BotToken": ""   // From the Bot page in Discord Developer Portal — treat as a secret
//   }
// }
// ============================================================================

// ============================================================================
// Program.cs — Registration (add after existing channel registrations):
// ============================================================================
//
// var discordConfig = builder.Configuration.GetSection("Channels:Discord");
// if (discordConfig.GetValue<bool>("Enabled"))
// {
//     builder.Services.AddSingleton<DiscordChannel>();
//     builder.Services.AddHostedService<DiscordBackgroundService>();
// }
//
// // After builder.Build(), inside the existing channel wiring scope:
// if (discordConfig.GetValue<bool>("Enabled"))
// {
//     var discord = scope.ServiceProvider.GetRequiredService<DiscordChannel>();
//     channelManager.RegisterChannel(discord);
// }
// ============================================================================
