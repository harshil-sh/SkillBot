# Configuration Reference

All SkillBot settings live in `SkillBot.Api/appsettings.json` (API) and `SkillBot.Console/appsettings.json` (console client). In production, use environment variables or a secrets manager instead of editing these files directly.

Environment variable names replace `:` with `__`, e.g. `JwtSettings:Secret` → `JwtSettings__Secret`.

---

## Table of Contents

1. [ConnectionStrings](#1-connectionstrings)
2. [JwtSettings](#2-jwtsettings)
3. [SkillBot](#3-skillbot)
   - [Caching](#caching)
4. [SerpApi](#4-serpapi)
5. [Channels — Telegram](#5-channels--telegram)
6. [Serilog / Logging](#6-serilog--logging)
7. [CORS](#7-cors)
8. [Console-specific Settings](#8-console-specific-settings)
9. [Environment Variable Quick Reference](#9-environment-variable-quick-reference)

---

## 1. ConnectionStrings

```json
"ConnectionStrings": {
  "SkillBot": "Data Source=skillbot.db"
}
```

| Key | Default | Description |
|---|---|---|
| `SkillBot` | `Data Source=skillbot.db` | SQLite connection string for user accounts and conversation history. Use an absolute path in production (e.g. `Data Source=/data/skillbot.db`). |

The cache database has its own path under [`SkillBot:Caching:CacheDatabasePath`](#caching).

---

## 2. JwtSettings

```json
"JwtSettings": {
  "Secret": "your-super-secret-key-min-32-chars-long-12345678",
  "Issuer": "SkillBot",
  "Audience": "SkillBotUsers",
  "ExpirationMinutes": 1440
}
```

| Key | Default | Required | Description |
|---|---|---|---|
| `Secret` | _(placeholder)_ | ✅ | HMAC-SHA256 signing key. **Must be ≥ 32 characters.** Change before deploying. |
| `Issuer` | `SkillBot` | | JWT `iss` claim. |
| `Audience` | `SkillBotUsers` | | JWT `aud` claim. |
| `ExpirationMinutes` | `1440` | | Token lifetime in minutes (default: 24 hours). |

> ⚠️ Never commit the real secret to source control. Use `dotnet user-secrets` or an environment variable.

---

## 3. SkillBot

```json
"SkillBot": {
  "ApiKey": "your-openai-api-key-here",
  "Model": "gpt-4",
  "AzureEndpoint": null,
  "AzureDeploymentName": null,
  "MaxHistoryMessages": 100,
  "VerboseLogging": false,
  "PluginAssemblyPaths": [],
  "MemoryProvider": "SQLite",
  "SqliteDatabasePath": "skillbot-api.db",
  "Caching": { ... }
}
```

| Key | Default | Description |
|---|---|---|
| `ApiKey` | _(placeholder)_ | Shared OpenAI API key used when a user has not set their own key. |
| `Model` | `gpt-4` | Default LLM model identifier. |
| `AzureEndpoint` | `null` | Azure OpenAI endpoint URL (leave `null` to use the public OpenAI API). |
| `AzureDeploymentName` | `null` | Azure OpenAI deployment name. |
| `MaxHistoryMessages` | `100` | Maximum number of messages kept in the Semantic Kernel chat history per session. |
| `VerboseLogging` | `false` | When `true`, emits additional debug-level logs from the agent engine. |
| `PluginAssemblyPaths` | `[]` | Paths to additional assemblies to scan for `[Plugin]`-decorated classes. |
| `MemoryProvider` | `SQLite` | Conversation memory backend: `SQLite` or `InMemory`. |
| `SqliteDatabasePath` | `skillbot-api.db` | Path to the SQLite file used by the memory provider. |

### Caching

```json
"Caching": {
  "Enabled": true,
  "CacheDatabasePath": "skillbot-cache.db",
  "MemoryCacheSizeMb": 100,
  "MaxCacheSizeMb": 500,
  "RoutingCacheTtl": "04:00:00",
  "AgentCacheTtl": "12:00:00",
  "GeneralCacheTtl": "1.00:00:00",
  "WebSearchTtl": "01:00:00",
  "NewsSearchTtl": "00:15:00",
  "ImageSearchTtl": "04:00:00",
  "CleanupInterval": "01:00:00",
  "EnableAutoCleanup": true
}
```

| Key | Default | Description |
|---|---|---|
| `Enabled` | `true` | Enable/disable the two-tier cache entirely. |
| `CacheDatabasePath` | `skillbot-cache.db` | Path to the SQLite file used for L2 persistent cache. |
| `MemoryCacheSizeMb` | `100` | Maximum size of the L1 in-memory cache in MB. |
| `MaxCacheSizeMb` | `500` | Maximum total size of the L2 SQLite cache in MB. |
| `RoutingCacheTtl` | `04:00:00` | TTL for multi-agent routing decisions (HH:mm:ss). |
| `AgentCacheTtl` | `12:00:00` | TTL for individual agent responses. |
| `GeneralCacheTtl` | `1.00:00:00` | Default TTL for general LLM responses (d.HH:mm:ss). |
| `WebSearchTtl` | `01:00:00` | TTL for web search results. |
| `NewsSearchTtl` | `00:15:00` | TTL for news search results (short because news changes rapidly). |
| `ImageSearchTtl` | `04:00:00` | TTL for image search results. |
| `CleanupInterval` | `01:00:00` | How often the background cleanup job runs. |
| `EnableAutoCleanup` | `true` | Automatically evict expired entries on the cleanup interval. |

---

## 4. SerpApi

```json
"SerpApi": {
  "ApiKey": "your-serpapi-key-here"
}
```

| Key | Required | Description |
|---|---|---|
| `ApiKey` | Optional | [SerpAPI](https://serpapi.com/) key. When set, the `SerpApiPlugin` is registered and web search is available to the agent. When `Caching.Enabled` is `true`, searches are automatically wrapped with the `CachedSerpApiPlugin`. |

---

## 5. Channels — Telegram

```json
"Channels": {
  "Telegram": {
    "Enabled": false,
    "BotToken": "",
    "BotUsername": "",
    "WebhookUrl": ""
  }
}
```

| Key | Default | Description |
|---|---|---|
| `Enabled` | `false` | Set to `true` to activate the Telegram channel. |
| `BotToken` | `""` | Bot token from @BotFather (e.g. `123456789:AAH...`). |
| `BotUsername` | `""` | Bot username without the `@` (e.g. `MySkillBot`). Informational only. |
| `WebhookUrl` | `""` | Public HTTPS URL that Telegram will POST updates to. Must include the path: `https://yourdomain.com/api/webhook/telegram`. |

---

## 6. Serilog / Logging

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "WriteTo": [
    {
      "Name": "Console",
      "Args": {
        "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
      }
    },
    {
      "Name": "File",
      "Args": {
        "path": "logs/skillbot-api-.log",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 7,
        "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
      }
    }
  ]
}
```

| Setting | Default | Description |
|---|---|---|
| `MinimumLevel.Default` | `Information` | Minimum log level for all sources. |
| `Override.Microsoft` | `Warning` | Suppress verbose ASP.NET Core framework logs. |
| `Override.System` | `Warning` | Suppress verbose .NET runtime logs. |
| `WriteTo[File].path` | `logs/skillbot-api-.log` | Log file path. The date is appended automatically when `rollingInterval` is set. |
| `WriteTo[File].rollingInterval` | `Day` | Create a new log file each day. |
| `WriteTo[File].retainedFileCountLimit` | `7` | Keep the last 7 daily log files. |

To add a Seq sink for centralised logging, add to `WriteTo`:

```json
{ "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } }
```

---

## 7. CORS

```json
"Cors": {
  "AllowedOrigins": "http://localhost:5000,https://localhost:5001"
}
```

Comma-separated list of allowed origins for the `AllowBlazorApp` CORS policy. Extend this list if you host a web front-end at a different origin.

---

## 8. Console-specific Settings

`SkillBot.Console/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7101"
}
```

| Key | Default | Description |
|---|---|---|
| `ApiBaseUrl` | `https://localhost:7101` | Base URL of the SkillBot API the console client connects to. |

Local console settings (JWT token, saved preferences) are stored outside the project at:
```
%APPDATA%\SkillBot\console-settings.json   (Windows)
~/.config/SkillBot/console-settings.json   (Linux/macOS)
```

---

## 9. Environment Variable Quick Reference

```bash
# Database
ConnectionStrings__SkillBot="Data Source=/data/skillbot.db"

# JWT
JwtSettings__Secret="change-me-min-32-chars-long-random-string"
JwtSettings__ExpirationMinutes="1440"

# LLM
SkillBot__ApiKey="sk-..."
SkillBot__Model="gpt-4"

# Caching
SkillBot__Caching__Enabled="true"
SkillBot__Caching__CacheDatabasePath="/data/skillbot-cache.db"

# Web search
SerpApi__ApiKey="..."

# Telegram
Channels__Telegram__Enabled="true"
Channels__Telegram__BotToken="123456:ABC..."
Channels__Telegram__WebhookUrl="https://yourdomain.com/api/webhook/telegram"

# CORS
Cors__AllowedOrigins="https://app.yourdomain.com"
```
