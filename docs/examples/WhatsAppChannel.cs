// ============================================================================
// WhatsAppChannel.cs — Reference implementation (NOT production-ready)
//
// This file is a skeleton showing how to integrate WhatsApp via the Twilio
// Programmable Messaging API. It is provided as a developer guide only.
//
// NuGet package required:
//   dotnet add package Twilio --version 7.x.x
//
// Twilio setup:
//   1. Create a Twilio account at https://twilio.com
//   2. Enable the WhatsApp sandbox (or a production number) in Twilio console
//   3. Copy your Account SID, Auth Token, and WhatsApp sender number
//   4. Add them to appsettings.json under "Channels:WhatsApp"
//
// Webhook setup:
//   - Twilio delivers incoming messages as HTTP POST to your webhook URL
//   - Create a WhatsAppWebhookController inheriting WebhookControllerBase
//   - Configure the webhook URL in Twilio console: https://yourdomain.com/api/webhooks/whatsapp
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using SkillBot.Infrastructure.Repositories;

// TODO: Add <PackageReference Include="Twilio" Version="7.x.x" /> to SkillBot.Infrastructure.csproj
// using Twilio;
// using Twilio.Rest.Api.V2010.Account;

namespace SkillBot.Infrastructure.Channels;

/// <summary>
/// WhatsApp channel via the Twilio Programmable Messaging API.
/// Incoming messages arrive as POST webhooks; outgoing messages use the Twilio REST client.
/// </summary>
public class WhatsAppChannel : BaseMessagingChannel
{
    private readonly string? _accountSid;
    private readonly string? _authToken;

    // Twilio WhatsApp sender — format: "whatsapp:+14155238886" (sandbox) or your approved number
    private readonly string? _fromNumber;

    public override string Name => "whatsapp";

    // Channel is usable only when all three Twilio credentials are present
    public override bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_accountSid) &&
        !string.IsNullOrWhiteSpace(_authToken) &&
        !string.IsNullOrWhiteSpace(_fromNumber);

    public WhatsAppChannel(
        IConfiguration configuration,
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger<WhatsAppChannel> logger)
        : base(channelUserRepository, userRepository, logger)
    {
        _accountSid = configuration["Channels:WhatsApp:AccountSid"];
        _authToken  = configuration["Channels:WhatsApp:AuthToken"];
        _fromNumber = configuration["Channels:WhatsApp:FromNumber"];

        // TODO: Initialise the Twilio client once credentials are loaded
        // if (IsConfigured)
        //     TwilioClient.Init(_accountSid, _authToken);
    }

    /// <summary>
    /// Sends a WhatsApp message to the given phone number via Twilio.
    /// </summary>
    /// <param name="userId">Recipient's E.164 phone number, e.g. "+447911123456".</param>
    /// <param name="message">Text content to send.</param>
    public override async Task<bool> SendMessageAsync(string userId, string message)
    {
        if (!IsConfigured)
        {
            Logger.LogWarning("WhatsApp channel is not configured; cannot send message to {UserId}", userId);
            return false;
        }

        // Twilio expects the WhatsApp prefix on both To and From numbers
        var toNumber   = userId.StartsWith("whatsapp:") ? userId : $"whatsapp:{userId}";
        var fromNumber = _fromNumber!.StartsWith("whatsapp:") ? _fromNumber : $"whatsapp:{_fromNumber}";

        try
        {
            // TODO: Uncomment once Twilio package is added
            // var messageResource = await MessageResource.CreateAsync(
            //     body: message,
            //     from: new Twilio.Types.PhoneNumber(fromNumber),
            //     to:   new Twilio.Types.PhoneNumber(toNumber)
            // );

            // TODO: Remove this placeholder log and uncomment the block above
            Logger.LogInformation(
                "[PLACEHOLDER] Would send WhatsApp message to {To}: {Message}", toNumber, message);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send WhatsApp message to {To}", toNumber);
            return false;
        }
    }

    /// <summary>
    /// Not used — Twilio delivers messages via webhook push (no polling needed).
    /// </summary>
    public override Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null);
}

// ============================================================================
// WhatsAppWebhookController.cs — Create this file in SkillBot.Api/Controllers/
// ============================================================================
//
// [ApiController]
// [Route("api/webhooks/whatsapp")]
// public class WhatsAppWebhookController : WebhookControllerBase
// {
//     private readonly IWebhookHandlerService _handler;
//
//     public WhatsAppWebhookController(
//         IChannelManager channelManager,
//         IWebhookHandlerService handler,
//         ILogger<WhatsAppWebhookController> logger)
//         : base(channelManager, logger)
//     {
//         _handler = handler;
//     }
//
//     // Twilio sends incoming WhatsApp messages as form-encoded POST
//     [HttpPost]
//     [Consumes("application/x-www-form-urlencoded")]
//     public async Task<IActionResult> Receive([FromForm] TwilioWebhookPayload payload)
//     {
//         // TODO: Optionally validate Twilio request signature using the auth token
//         // https://www.twilio.com/docs/usage/webhooks/webhooks-security
//
//         // Twilio sends phone numbers with the "whatsapp:" prefix; strip it for storage
//         var senderPhone = payload.From?.Replace("whatsapp:", "") ?? string.Empty;
//
//         var message = new Message
//         {
//             Id          = Guid.NewGuid().ToString(),
//             UserId      = senderPhone,
//             Text        = payload.Body ?? string.Empty,
//             ChannelName = "whatsapp",
//             ReceivedAt  = DateTime.UtcNow
//         };
//
//         await ProcessIncomingMessage(message);
//
//         // Return TwiML empty response — Twilio requires a valid XML reply
//         return Content("<Response/>", "text/xml");
//     }
// }
//
// // Minimal model for Twilio's form-encoded webhook payload
// public class TwilioWebhookPayload
// {
//     public string? From { get; set; }  // e.g. "whatsapp:+447911123456"
//     public string? Body { get; set; }  // Message text
// }
// ============================================================================

// ============================================================================
// appsettings.json — Add the following section:
// ============================================================================
//
// "Channels": {
//   "WhatsApp": {
//     "Enabled": false,
//     "AccountSid": "",        // From Twilio console dashboard
//     "AuthToken": "",         // From Twilio console dashboard — treat as a secret
//     "FromNumber": "+14155238886"  // Sandbox number or your approved WhatsApp number
//   }
// }
// ============================================================================

// ============================================================================
// Program.cs — Registration (add after existing channel registrations):
// ============================================================================
//
// var whatsAppConfig = builder.Configuration.GetSection("Channels:WhatsApp");
// if (whatsAppConfig.GetValue<bool>("Enabled"))
// {
//     builder.Services.AddSingleton<WhatsAppChannel>();
// }
//
// // After builder.Build(), inside the existing channel wiring scope:
// if (whatsAppConfig.GetValue<bool>("Enabled"))
// {
//     var whatsApp = scope.ServiceProvider.GetRequiredService<WhatsAppChannel>();
//     channelManager.RegisterChannel(whatsApp);
// }
// ============================================================================
