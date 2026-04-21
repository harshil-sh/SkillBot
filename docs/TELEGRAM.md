# Telegram Bot Setup Guide

SkillBot ships with a built-in Telegram channel that lets users chat with the AI assistant, switch LLM providers, and manage their API keys — all from within Telegram.

---

## Table of Contents

1. [How It Works](#1-how-it-works)
2. [Creating a Bot with @BotFather](#2-creating-a-bot-with-botfather)
3. [Configuring SkillBot](#3-configuring-skillbot)
4. [Setting Up a Webhook](#4-setting-up-a-webhook)
   - [Development (ngrok)](#development-ngrok)
   - [Production](#production)
5. [Available Commands](#5-available-commands)
6. [User Registration Flow](#6-user-registration-flow)
7. [Production Deployment Notes](#7-production-deployment-notes)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. How It Works

```
Telegram user  ──message──▶  Telegram servers
                                    │
                          POST /api/webhook/telegram
                                    │
                     TelegramWebhookController
                                    │
                     ┌──────────────▼──────────────┐
                     │  Slash command?               │
                     │  Yes → TelegramChannel        │
                     │         .HandleCommandAsync   │
                     │  No  → WebhookHandlerService  │
                     │         → IAgentEngine        │
                     └──────────────────────────────┘
                                    │
                     TelegramChannel.SendMessageAsync
                                    │
Telegram user  ◀──reply────  Telegram servers
```

The bot uses the **webhook** model: Telegram POSTs each incoming update to your API. You do not need to poll or keep a persistent connection open.

---

## 2. Creating a Bot with @BotFather

1. Open Telegram and search for **@BotFather**.
2. Send `/newbot`.
3. Choose a display name (e.g. `My SkillBot`).
4. Choose a username ending in `bot` (e.g. `my_skillbot`). This must be unique across Telegram.
5. BotFather replies with your **HTTP API token** — it looks like:
   ```
   123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw
   ```
6. Copy this token; you will need it in the next step.

Optional but recommended: set a description and profile photo for your bot:

```
/setdescription — Add a description shown in the bot's profile
/setuserpic     — Upload a profile photo
/setcommands    — Register the command list shown in the menu
```

Suggested command list for `/setcommands`:
```
start - Welcome message
help - Show available commands
settings - View your current settings
setkey - Set an API key
setprovider - Change LLM provider
search - Web search
multi - Multi-agent response
```

---

## 3. Configuring SkillBot

### Using `dotnet user-secrets` (development)

```bash
cd SkillBot.Api
dotnet user-secrets set "Channels:Telegram:Enabled"     "true"
dotnet user-secrets set "Channels:Telegram:BotToken"    "123456789:AAH..."
dotnet user-secrets set "Channels:Telegram:BotUsername" "my_skillbot"
dotnet user-secrets set "Channels:Telegram:WebhookUrl"  "https://your-public-url/api/webhook/telegram"
```

### Using environment variables (production)

```bash
export Channels__Telegram__Enabled="true"
export Channels__Telegram__BotToken="123456789:AAH..."
export Channels__Telegram__BotUsername="my_skillbot"
export Channels__Telegram__WebhookUrl="https://yourdomain.com/api/webhook/telegram"
```

### Editing `appsettings.json`

```json
"Channels": {
  "Telegram": {
    "Enabled": true,
    "BotToken": "123456789:AAH...",
    "BotUsername": "my_skillbot",
    "WebhookUrl": "https://yourdomain.com/api/webhook/telegram"
  }
}
```

---

## 4. Setting Up a Webhook

Telegram requires the webhook URL to be **publicly accessible over HTTPS**. Self-signed certificates are not supported for production but are accepted for testing when registered with the Telegram API.

### Development (ngrok)

[ngrok](https://ngrok.com) creates a temporary public tunnel to your local machine.

```bash
# 1. Install ngrok (https://ngrok.com/download)

# 2. Start your SkillBot API
dotnet run --project SkillBot.Api/SkillBot.Api.csproj

# 3. In a separate terminal, start ngrok on the HTTP port
ngrok http 5188

# ngrok output will show something like:
# Forwarding  https://abc123.ngrok-free.app -> http://localhost:5188

# 4. Set the WebhookUrl to the ngrok HTTPS URL
dotnet user-secrets set "Channels:Telegram:WebhookUrl" \
  "https://abc123.ngrok-free.app/api/webhook/telegram"

# 5. Restart the API so it picks up the new URL
# 6. Register the webhook with Telegram
curl "https://api.telegram.org/bot<YOUR_TOKEN>/setWebhook?url=https://abc123.ngrok-free.app/api/webhook/telegram"
```

> ⚠️ ngrok URLs change every time you restart unless you use a paid plan. Update `WebhookUrl` and re-register the webhook after each restart.

### Verifying the webhook

```bash
curl "https://api.telegram.org/bot<YOUR_TOKEN>/getWebhookInfo"
```

A successful response looks like:

```json
{
  "ok": true,
  "result": {
    "url": "https://abc123.ngrok-free.app/api/webhook/telegram",
    "has_custom_certificate": false,
    "pending_update_count": 0,
    "last_error_date": null,
    "last_error_message": null
  }
}
```

---

## 5. Available Commands

| Command | Description |
|---|---|
| `/start` | Welcome message with a brief introduction |
| `/help` | List all available commands |
| `/settings` | Show your current provider and which API keys are set |
| `/setkey <provider> <key>` | Store an API key for a provider |
| `/setprovider <provider>` | Switch the LLM provider used for your messages |
| `/search <query>` | Perform a web search (requires `SerpApi:ApiKey`) |
| `/multi <message>` | Send a message to the multi-agent orchestrator |

### `/setkey` providers

| Provider | Identifier |
|---|---|
| OpenAI | `openai` |
| Anthropic Claude | `claude` |
| Google Gemini | `gemini` |
| SerpAPI | `serpapi` |

**Example:**

```
/setkey openai sk-proj-abc123...
/setprovider claude
```

### Chat without a command

Any text that does not start with `/` is forwarded to the agent engine as a chat message. The response is sent back to the same Telegram chat.

Long responses are automatically split into multiple messages (Telegram has a 4096-character limit per message).

---

## 6. User Registration Flow

When a Telegram user sends their first message, SkillBot automatically:

1. Creates a **channel user** record mapping the Telegram chat ID to a SkillBot system user.
2. Creates a SkillBot account for the user if one does not already exist.
3. Subsequent messages are processed under that account, with all per-user settings (API keys, preferred provider) applied.

The mapping is stored in the `ChannelUsers` table in `skillbot.db`.

---

## 7. Production Deployment Notes

### Use a permanent domain

Register a permanent domain (or subdomain) with a valid TLS certificate. Services like Let's Encrypt provide free certificates.

### Register the webhook once

After deploying, register the webhook with Telegram once:

```bash
curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://yourdomain.com/api/webhook/telegram"}'
```

### Secure the endpoint (optional)

To ensure that only Telegram can call your webhook, validate the `X-Telegram-Bot-Api-Secret-Token` header:

1. Set a secret token when registering the webhook:
   ```bash
   curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
     -H "Content-Type: application/json" \
     -d '{"url": "...", "secret_token": "my-random-secret"}'
   ```
2. In `TelegramWebhookController`, verify the header value matches.

### Rate limiting

SkillBot's rate limiter applies per-user limits to chat requests. Telegram messages are routed through `WebhookHandlerService`, which calls the agent engine. The same rate limits apply to Telegram users as to API users.

### Removing or changing the webhook

```bash
# Remove webhook (switch to polling mode)
curl "https://api.telegram.org/bot<TOKEN>/deleteWebhook"

# Update webhook URL
curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
  -d '{"url": "https://new-url.com/api/webhook/telegram"}'
```

---

## 8. Troubleshooting

### Bot does not respond

1. Check that `Channels:Telegram:Enabled` is `true` and the API has been restarted.
2. Verify the webhook is registered: `getWebhookInfo` should show your URL with no errors.
3. Look for errors in `logs/skillbot-api-<date>.log`.

### `Invalid token` error on startup

The `BotToken` value is incorrect or has been revoked. Generate a new one with `/revoke` in BotFather.

### Messages delivered but no reply

The agent engine may have encountered an error. Check the API logs for exceptions during `WebhookHandlerService.HandleMessageAsync`.

### `Telegram channel is not configured`

The `BotToken` is empty. Ensure it is set and that the application was restarted after the change.
