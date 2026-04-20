// ============================================================================
// SlackChannel.cs — Reference implementation (NOT production-ready)
//
// This file is a skeleton showing how to integrate Slack using the
// SlackNet library (the most actively maintained .NET Slack SDK).
//
// NuGet packages required:
//   dotnet add package SlackNet --version 2.x.x
//   dotnet add package SlackNet.AspNetCore --version 2.x.x
//
// Slack app setup:
//   1. Create a Slack app at https://api.slack.com/apps
//   2. Under "OAuth & Permissions", add the following Bot Token Scopes:
//        chat:write       — post messages
//        im:history       — read DMs (for direct message channels)
//        im:read          — list DMs
//   3. Enable "Event Subscriptions" and set the Request URL to:
//        https://yourdomain.com/api/webhooks/slack
//   4. Subscribe to bot events: message.im (DMs) and/or message.channels
//   5. Install the app to your workspace and copy the Bot User OAuth Token
//      and the Signing Secret from the app settings
//
// Webhook setup:
//   - Slack sends a URL verification challenge on first subscription (handled below)
//   - Subsequent events are delivered as JSON POST to your webhook URL
//   - SlackNet.AspNetCore provides middleware to handle signature verification
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using SkillBot.Infrastructure.Repositories;

// TODO: Add SlackNet packages to SkillBot.Infrastructure.csproj
// using SlackNet;
// using SlackNet.WebApi;

namespace SkillBot.Infrastructure.Channels;

/// <summary>
/// Slack channel via SlackNet.
/// Incoming messages arrive as POST event webhooks; outgoing messages use the Slack Web API.
/// </summary>
public class SlackChannel : BaseMessagingChannel
{
    // Bot User OAuth Token — starts with "xoxb-"
    private readonly string? _botToken;

    // Signing Secret — used by SlackNet middleware to verify incoming requests
    private readonly string? _signingSecret;

    public override string Name => "slack";

    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_botToken) &&
        !string.IsNullOrWhiteSpace(_signingSecret);

    public SlackChannel(
        IConfiguration configuration,
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger<SlackChannel> logger)
        : base(channelUserRepository, userRepository, logger)
    {
        _botToken      = configuration["Channels:Slack:BotToken"];
        _signingSecret = configuration["Channels:Slack:SigningSecret"];

        // TODO: Initialise the SlackNet API client
        // if (IsConfigured)
        //     _slackClient = new SlackServiceBuilder()
        //         .UseApiToken(_botToken)
        //         .GetApiClient();
    }

    /// <summary>
    /// Posts a message to a Slack channel or DM.
    /// </summary>
    /// <param name="userId">Slack channel ID or user ID, e.g. "C01234ABCDE" or "U01234ABCDE".</param>
    /// <param name="message">Text content to send (supports mrkdwn formatting).</param>
    public override async Task<bool> SendMessageAsync(string userId, string message)
    {
        if (!IsConfigured)
        {
            Logger.LogWarning("Slack channel is not configured; cannot send message to {UserId}", userId);
            return false;
        }

        try
        {
            // TODO: Uncomment once SlackNet is added
            // await _slackClient.Chat.PostMessage(new Message
            // {
            //     Channel = userId,
            //     Text    = message
            // });

            // TODO: Remove this placeholder log and uncomment the block above
            Logger.LogInformation(
                "[PLACEHOLDER] Would send Slack message to {Channel}: {Message}", userId, message);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send Slack message to {Channel}", userId);
            return false;
        }
    }

    /// <summary>
    /// Not used — Slack delivers messages via event webhook push (no polling needed).
    /// </summary>
    public override Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null);
}

// ============================================================================
// SlackWebhookController.cs — Create this file in SkillBot.Api/Controllers/
// ============================================================================
//
// [ApiController]
// [Route("api/webhooks/slack")]
// public class SlackWebhookController : WebhookControllerBase
// {
//     private readonly IWebhookHandlerService _handler;
//
//     public SlackWebhookController(
//         IChannelManager channelManager,
//         IWebhookHandlerService handler,
//         ILogger<SlackWebhookController> logger)
//         : base(channelManager, logger)
//     {
//         _handler = handler;
//     }
//
//     [HttpPost]
//     public async Task<IActionResult> Receive([FromBody] SlackEventPayload payload)
//     {
//         // Slack sends a one-time URL verification challenge when you first set the webhook
//         if (payload.Type == "url_verification")
//             return Ok(new { challenge = payload.Challenge });
//
//         // TODO: Verify request signature using the Signing Secret
//         // SlackNet.AspNetCore middleware can handle this automatically if registered in Program.cs
//
//         // Only process message events (ignore bot messages to avoid loops)
//         if (payload.Event?.Type != "message" || payload.Event.BotId is not null)
//             return Ok();
//
//         var message = new Message
//         {
//             Id          = payload.Event.ClientMsgId ?? Guid.NewGuid().ToString(),
//             UserId      = payload.Event.User ?? string.Empty,  // Slack user ID
//             Text        = payload.Event.Text ?? string.Empty,
//             ChannelName = "slack",
//             ReceivedAt  = DateTime.UtcNow
//         };
//
//         await ProcessIncomingMessage(message);
//
//         // Slack requires a 200 OK response within 3 seconds; for slow AI responses
//         // consider using a background queue and acknowledging immediately
//         return Ok();
//     }
// }
//
// // Minimal models for Slack's event API payload (use SlackNet types in production)
// public class SlackEventPayload
// {
//     public string? Type      { get; set; }  // "url_verification" or "event_callback"
//     public string? Challenge { get; set; }  // Present only for url_verification
//     public SlackEvent? Event { get; set; }
// }
//
// public class SlackEvent
// {
//     public string? Type       { get; set; }  // e.g. "message"
//     public string? User       { get; set; }  // Slack user ID, e.g. "U01234ABCDE"
//     public string? Text       { get; set; }  // Message text
//     public string? Channel    { get; set; }  // Channel or DM ID
//     public string? BotId      { get; set; }  // Non-null when sent by a bot — use to filter loops
//     public string? ClientMsgId { get; set; } // Deduplification key
// }
// ============================================================================

// ============================================================================
// appsettings.json — Add the following section:
// ============================================================================
//
// "Channels": {
//   "Slack": {
//     "Enabled": false,
//     "BotToken": "",       // "xoxb-..." from OAuth & Permissions in Slack app settings
//     "SigningSecret": ""   // From Basic Information in Slack app settings — treat as a secret
//   }
// }
// ============================================================================

// ============================================================================
// Program.cs — Registration (add after existing channel registrations):
// ============================================================================
//
// var slackConfig = builder.Configuration.GetSection("Channels:Slack");
// if (slackConfig.GetValue<bool>("Enabled"))
// {
//     builder.Services.AddSingleton<SlackChannel>();
// }
//
// // After builder.Build(), inside the existing channel wiring scope:
// if (slackConfig.GetValue<bool>("Enabled"))
// {
//     var slack = scope.ServiceProvider.GetRequiredService<SlackChannel>();
//     channelManager.RegisterChannel(slack);
// }
// ============================================================================
