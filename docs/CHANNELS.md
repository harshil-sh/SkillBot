# Adding New Messaging Channels

## Table of Contents
- [Overview](#overview)
- [Existing Channels](#existing-channels)
- [Creating a New Channel](#creating-a-new-channel)
- [Channel Examples](#channel-examples)
- [Testing Your Channel](#testing-your-channel)

---

## Overview

SkillBot supports multiple messaging channels through a common `IMessagingChannel` interface. Channels receive messages via webhooks (the external platform calls your API) and send replies back via the platform's HTTP API.

```
External Platform  ──webhook──▶  WebhookController  ──▶  WebhookHandlerService
                                                                  │
                                                         IAgentEngine.ExecuteAsync
                                                                  │
                                                    IChannelManager.GetChannel(name)
                                                                  │
                                                    IMessagingChannel.SendMessageAsync
                                                                  │
                   External Platform  ◀──reply───────────────────┘
```

### Key Abstractions

| Type | Location | Purpose |
|---|---|---|
| `IMessagingChannel` | `SkillBot.Core/Services` | Contract every channel must implement |
| `BaseMessagingChannel` | `SkillBot.Infrastructure/Channels` | Shared user-mapping logic; extend this |
| `IChannelManager` | `SkillBot.Infrastructure/Channels` | In-memory registry of active channels |
| `ChannelConfiguration` | `SkillBot.Core/Models` | Typed config model for appsettings |
| `ChannelUser` | `SkillBot.Core/Models` | Maps channel-native IDs to system users |
| `WebhookHandlerService` | `SkillBot.Api/Services` | Orchestrates the full inbound message flow |

---

## Existing Channels

| Channel | Status | Notes |
|---|---|---|
| **Telegram** | Built-in | Uses `Telegram.Bot` NuGet package; webhook-based |
| **WhatsApp** | Example | Twilio provider; see guide below |
| **Slack** | Example | Bolt SDK; see guide below |
| **Discord** | Example | Discord.Net; see guide below |

---

## Creating a New Channel

### 1. Implement `IMessagingChannel` directly (minimal)

Use this when you don't need the shared user-registration logic.

```csharp
using SkillBot.Core.Services;

public class MyChannel : IMessagingChannel
{
    public string Name => "mychannel";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    private readonly string? _apiKey;

    public MyChannel(IConfiguration configuration)
    {
        _apiKey = configuration["Channels:MyChannel:ApiKey"];
    }

    public async Task<bool> SendMessageAsync(string userId, string message)
    {
        // Call the platform's HTTP API here
        return true;
    }

    public Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null); // Use webhooks instead

    public Task<bool> RegisterUserAsync(string channelUserId, string systemUserId) =>
        Task.FromResult(true);

    public Task<User?> GetUserByChannelIdAsync(string channelUserId) =>
        Task.FromResult<User?>(null);
}
```

### 2. Inherit from `BaseMessagingChannel` (recommended)

`BaseMessagingChannel` handles `RegisterUserAsync` and `GetUserByChannelIdAsync` for you using `IChannelUserRepository`. Only `SendMessageAsync` and `ReceiveMessageAsync` need to be implemented.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using SkillBot.Infrastructure.Repositories;

public class MyChannel : BaseMessagingChannel
{
    private readonly string? _apiKey;

    public override string Name => "mychannel";
    public override bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public MyChannel(
        IConfiguration configuration,
        IChannelUserRepository channelUserRepository,
        IUserRepository userRepository,
        ILogger<MyChannel> logger)
        : base(channelUserRepository, userRepository, logger)
    {
        _apiKey = configuration["Channels:MyChannel:ApiKey"];
    }

    public override async Task<bool> SendMessageAsync(string userId, string message)
    {
        if (!IsConfigured)
        {
            Logger.LogWarning("MyChannel is not configured");
            return false;
        }

        try
        {
            // Send message using the platform API
            Logger.LogDebug("Sent message to {UserId} on MyChannel", userId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send message to {UserId}", userId);
            return false;
        }
    }

    // Webhooks push messages in; polling not needed
    public override Task<Message?> ReceiveMessageAsync() =>
        Task.FromResult<Message?>(null);
}
```

### 3. Add Configuration

Add a section to `SkillBot.Api/appsettings.json` and `appsettings.Development.json`:

```json
{
  "Channels": {
    "MyChannel": {
      "Enabled": false,
      "ApiKey": "",
      "WebhookUrl": ""
    }
  }
}
```

Set values via environment variables in production (avoids storing secrets in source):

```bash
Channels__MyChannel__Enabled=true
Channels__MyChannel__ApiKey=your-key-here
```

### 4. Register Channel in `Program.cs`

Add the service registration **before** `builder.Build()`:

```csharp
// Register MyChannel if enabled
if (builder.Configuration.GetValue<bool>("Channels:MyChannel:Enabled"))
{
    builder.Services.AddSingleton<MyChannel>();
}
```

Wire it into `IChannelManager` **after** `builder.Build()`, alongside the other channel registrations:

```csharp
using (var scope = app.Services.CreateScope())
{
    var channelManager = scope.ServiceProvider.GetRequiredService<IChannelManager>();

    if (app.Configuration.GetValue<bool>("Channels:MyChannel:Enabled"))
    {
        var myChannel = app.Services.GetRequiredService<MyChannel>();
        channelManager.RegisterChannel(myChannel);
        app.Logger.LogInformation("MyChannel registered");
    }
}
```

### 5. Create a Webhook Controller

Inherit from `WebhookControllerBase` and add an endpoint that your platform calls:

```csharp
using Microsoft.AspNetCore.Mvc;
using SkillBot.Api.Services;
using SkillBot.Core.Services;
using SkillBot.Infrastructure.Channels;
using SkillBot.Core.Interfaces;

[Route("api/webhooks/mychannel")]
public class MyChannelWebhookController : WebhookControllerBase
{
    private readonly IWebhookHandlerService _webhookHandler;

    public MyChannelWebhookController(
        IChannelManager channelManager,
        IAgentEngine agentEngine,
        IWebhookHandlerService webhookHandler,
        ILogger<MyChannelWebhookController> logger)
        : base(channelManager, agentEngine, logger)
    {
        _webhookHandler = webhookHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] MyChannelUpdate update)
    {
        // Map the platform's payload to SkillBot's Message record
        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            UserId = update.SenderId,
            Text = update.Text,
            ChannelName = "mychannel",
            ReceivedAt = DateTime.UtcNow
        };

        await _webhookHandler.HandleMessageAsync(message);
        return Ok();
    }
}
```

---

## Channel Examples

### WhatsApp (Twilio)

**NuGet:** `Twilio`

**appsettings.json:**
```json
"Channels": {
  "WhatsApp": {
    "Enabled": false,
    "AccountSid": "",
    "AuthToken": "",
    "FromNumber": "whatsapp:+14155238886"
  }
}
```

**Key implementation:**
```csharp
public override string Name => "whatsapp";

public override bool IsConfigured =>
    !string.IsNullOrWhiteSpace(_accountSid) &&
    !string.IsNullOrWhiteSpace(_authToken);

public override async Task<bool> SendMessageAsync(string userId, string message)
{
    TwilioClient.Init(_accountSid, _authToken);

    await MessageResource.CreateAsync(
        body: message,
        from: new Twilio.Types.PhoneNumber(_fromNumber),
        to: new Twilio.Types.PhoneNumber($"whatsapp:{userId}"));

    return true;
}
```

**Webhook route:** `POST /api/webhooks/whatsapp`  
Twilio sends form-encoded data; read `Body` and `From` fields from the request.

---

### Slack (Bolt SDK)

**NuGet:** `SlackNet.AspNetCore`

**appsettings.json:**
```json
"Channels": {
  "Slack": {
    "Enabled": false,
    "BotToken": "",
    "SigningSecret": "",
    "AppToken": ""
  }
}
```

**Key implementation:**
```csharp
public override string Name => "slack";

public override bool IsConfigured =>
    !string.IsNullOrWhiteSpace(_botToken);

public override async Task<bool> SendMessageAsync(string userId, string message)
{
    var response = await _slackClient.Chat.PostMessage(new Message
    {
        Channel = userId,
        Text = message
    });

    return response.Ok;
}
```

**Webhook route:** `POST /api/webhooks/slack`  
Validate the `X-Slack-Signature` header before processing. Slack sends JSON event payloads.

---

### Discord (Discord.Net)

**NuGet:** `Discord.Net`

**appsettings.json:**
```json
"Channels": {
  "Discord": {
    "Enabled": false,
    "BotToken": "",
    "GuildId": ""
  }
}
```

**Key implementation:**
```csharp
public override string Name => "discord";

public override bool IsConfigured =>
    !string.IsNullOrWhiteSpace(_botToken);

public override async Task<bool> SendMessageAsync(string userId, string message)
{
    // userId is the Discord channel/user snowflake ID
    if (!ulong.TryParse(userId, out var channelId))
        return false;

    var channel = _discordClient.GetChannel(channelId) as IMessageChannel;
    if (channel is null)
        return false;

    await channel.SendMessageAsync(message);
    return true;
}
```

**Note:** Discord uses a persistent WebSocket gateway, not an HTTP webhook. Start the `DiscordSocketClient` as an `IHostedService` and feed messages into `IWebhookHandlerService.HandleMessageAsync`.

---

## Testing Your Channel

### 1. Verify configuration loads

```csharp
var config = app.Configuration.GetSection("Channels:MyChannel");
Console.WriteLine($"Enabled: {config.GetValue<bool>("Enabled")}");
Console.WriteLine($"ApiKey set: {!string.IsNullOrEmpty(config["ApiKey"])}");
```

### 2. Check channel registration

Call the health endpoint and confirm the channel appears in logs at startup:

```
[INF] MyChannel registered
```

### 3. Test `SendMessageAsync` in isolation

```csharp
// In a throwaway console app or integration test
var channel = serviceProvider.GetRequiredService<MyChannel>();
var sent = await channel.SendMessageAsync("test-user-id", "Hello from SkillBot!");
Console.WriteLine($"Sent: {sent}");
```

### 4. Simulate an inbound webhook

Use `curl` or a tool like [ngrok](https://ngrok.com) to expose your local API and trigger a real webhook from the platform:

```bash
# Example for a JSON payload channel
curl -X POST https://localhost:7101/api/webhooks/mychannel \
  -H "Content-Type: application/json" \
  -d '{"senderId": "user123", "text": "Hello!"}'
```

### 5. Verify end-to-end flow

Check logs for the full pipeline:
```
[INF] Handling webhook message {id} from user123 on mychannel
[DBG] Resolved system user {systemId} for channel user user123
[INF] Auto-registered channel user user123 on mychannel as system user {systemId}
[DBG] Sent message to user123 on mychannel
```
