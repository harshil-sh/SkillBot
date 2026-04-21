# Installation Guide

This guide walks you through installing and running SkillBot from source.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Clone the Repository](#clone-the-repository)
3. [Configuration](#configuration)
4. [Database Setup](#database-setup)
5. [Running the API](#running-the-api)
6. [Running the Console Client](#running-the-console-client)
7. [Docker (Coming Soon)](#docker-coming-soon)
8. [Troubleshooting](#troubleshooting)

---

## 1. Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | Required |
| Git | any | Required |
| SQLite | bundled | Included via `Microsoft.Data.Sqlite`; no separate install needed |
| LLM API key | — | OpenAI, Anthropic Claude, or Google Gemini |
| SerpAPI key | — | Optional, for web search |
| IDE | any | Visual Studio 2022, VS Code, or JetBrains Rider recommended |

Verify your .NET installation:

```bash
dotnet --version
# Expected: 10.0.x
```

---

## 2. Clone the Repository

```bash
git clone https://github.com/harshil-sh/SkillBot.git
cd SkillBot
```

Restore NuGet packages:

```bash
dotnet restore SkillBot.slnx
```

---

## 3. Configuration

SkillBot reads configuration from `appsettings.json`, environment variables, and (in development) `dotnet user-secrets`.

### Option A — `dotnet user-secrets` (recommended for development)

```bash
cd SkillBot.Api

# Required: LLM API key
dotnet user-secrets set "SkillBot:ApiKey" "sk-your-openai-key"

# Required: JWT signing secret (min 32 characters)
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-min-32-chars-long"

# Optional: SerpAPI web search
dotnet user-secrets set "SerpApi:ApiKey" "your-serpapi-key"

# Optional: Telegram bot
dotnet user-secrets set "Channels:Telegram:BotToken" "123456:ABC-DEF..."
dotnet user-secrets set "Channels:Telegram:Enabled" "true"
dotnet user-secrets set "Channels:Telegram:WebhookUrl" "https://your-domain/api/webhook/telegram"
```

### Option B — Environment variables

Environment variables use double-underscore (`__`) as the section separator:

```bash
export SkillBot__ApiKey="sk-your-openai-key"
export JwtSettings__Secret="your-super-secret-key-min-32-chars-long"
export SerpApi__ApiKey="your-serpapi-key"
export Channels__Telegram__BotToken="123456:ABC-DEF..."
export Channels__Telegram__Enabled="true"
```

### Option C — Edit `appsettings.json` directly

> ⚠️ Do not commit secrets to source control. Use environment variables or user-secrets in production.

Edit `SkillBot.Api/appsettings.json` and fill in the placeholder values. See [CONFIGURATION.md](CONFIGURATION.md) for all available settings.

---

## 4. Database Setup

SkillBot uses SQLite and sets up its schema automatically on first launch. No manual migration steps are needed.

Two database files are created in the API working directory:

| File | Purpose |
|---|---|
| `skillbot-api.db` | User accounts, conversation history |
| `skillbot-cache.db` | L2 persistent cache for LLM responses and search results |

To inspect the databases, use the [DB Browser for SQLite](https://sqlitebrowser.org/) or the `sqlite3` CLI.

---

## 5. Running the API

```bash
dotnet run --project SkillBot.Api/SkillBot.Api.csproj
```

Default URLs:

- HTTPS: `https://localhost:7101`
- HTTP:  `http://localhost:5188`
- Swagger UI (development): `https://localhost:7101/`
- Hangfire Dashboard: `https://localhost:7101/hangfire`
- Health Check: `https://localhost:7101/health`

To change the ports, edit `SkillBot.Api/Properties/launchSettings.json`.

### Development profile

The `Development` environment enables:
- Swagger UI at the root URL
- Detailed error responses
- `Debug`-level logging for `SkillBot.*` namespaces

---

## 6. Running the Console Client

The console client connects to the API at `https://localhost:7101` by default.

```bash
# Single-agent mode
dotnet run --project SkillBot.Console/SkillBot.Console.csproj

# Multi-agent mode
dotnet run --project SkillBot.Console/SkillBot.Console.csproj -- --multi-agent
```

The console base URL is configured in `SkillBot.Console/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7101"
}
```

Console settings (auth tokens, preferences) are stored per-user in:
```
%APPDATA%\SkillBot\console-settings.json
```

---

## 7. Docker (Coming Soon)

Docker support is planned. A `Dockerfile` and `docker-compose.yml` will be added in a future release.

Planned usage:

```bash
# Build and start
docker compose up --build

# Stop
docker compose down
```

---

## 8. Troubleshooting

### `SSL certificate error` when running locally

The .NET dev certificate may not be trusted. Run:

```bash
dotnet dev-certs https --trust
```

### `Database is locked`

Only one process should write to the SQLite databases at a time. Ensure you do not have multiple API instances pointing at the same `skillbot-api.db`. The cache database uses WAL mode for improved concurrency.

### `Plugin not called`

1. Verify the `[Description]` on the plugin class, method, and parameters is clear and descriptive.
2. Check that the plugin was registered at startup (look for `"Registering plugins..."` in the logs).
3. Ensure `SerpApi:ApiKey` is set if you expect web search to be available.

### `Multi-agent mode hangs`

Check that all four specialized agents (`ResearchAgent`, `CodingAgent`, `DataAnalysisAgent`, `WritingAgent`) are registered in the DI container via `AddMultiAgentOrchestration()`.

### `401 Unauthorized` from API

The JWT token has expired (default: 24 hours). Log in again to get a new token.

### Rate limit exceeded

The default rate limiter allows a certain number of requests per user per minute. Wait for the `Retry-After` period or adjust `RateLimiter` configuration.

### Log files

API logs are written to `logs/skillbot-api-<date>.log` (last 7 days retained).
