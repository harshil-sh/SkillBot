# 🤖 SkillBot — Self-Hosted AI Assistant

![Build](https://img.shields.io/badge/build-passing-brightgreen)
![License](https://img.shields.io/badge/license-MIT-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![C#](https://img.shields.io/badge/C%23-12-blue)

**SkillBot** is a production-ready, self-hosted AI assistant built with .NET 10 and Microsoft Semantic Kernel. It supports multiple LLM providers (OpenAI, Claude, Gemini), a Telegram bot interface, a REST API, a console client, multi-agent orchestration, and a plugin system — all in one deployable package.

---

## ✨ Features

- 🧠 **Multi-LLM Support** — OpenAI (GPT-4), Anthropic Claude, and Google Gemini; switch per-user at runtime
- 🤖 **Multi-Agent Orchestration** — Route complex tasks to specialised agents (Research, Coding, Data Analysis, Writing)
- 📱 **Telegram Bot** — Full-featured bot with slash commands, per-user keys, and provider switching
- 🌐 **REST API** — ASP.NET Core with JWT authentication, rate limiting, and Swagger UI
- 💻 **Console Client** — Interactive terminal interface for local use
- 🔌 **Plugin System** — Reflection-based discovery with `[Plugin]`, `[KernelFunction]`, `[Description]` attributes
- 🔍 **Web Search** — SerpAPI integration with caching
- ⚡ **Two-Tier Caching** — In-memory (L1) + SQLite (L2) cache for LLM responses and search results
- 🛡️ **Security** — JWT auth, rate limiting, input validation, content safety checks
- ⏰ **Background Tasks** — Hangfire-powered one-time and recurring task scheduling
- 📊 **Token Tracking** — Per-conversation usage statistics
- 💾 **SQLite Persistence** — Conversation history and user settings
- 📝 **Structured Logging** — Serilog with rolling file and console sinks
- 🖥️ **Web UI** — Blazor WebAssembly SPA with MudBlazor, dark mode, chat, settings, and admin dashboard

---

## 🖥️ Web Interface

SkillBot includes a modern **Blazor WebAssembly** single-page application that provides a full-featured browser-based UI without requiring any server-side rendering.

### Web UI Features

- 💬 **Chat Interface** — Conversational AI with markdown rendering and conversation history
- 🤖 **Multi-Agent Mode** — Toggle between single-agent and multi-agent orchestration in one click
- ⚙️ **Settings Dashboard** — Configure API keys, preferred LLM provider, and appearance
- 🎨 **Dark / Light Mode** — Theme persisted in `localStorage`, no flash on reload
- 👤 **Account Management** — Profile, password change, and account deletion
- 🛡️ **Admin Dashboard** — User management, usage analytics, and system monitoring
- 📱 **Responsive** — Works on desktop, tablet, and mobile browsers

### Access URLs

| Service | Development URL |
|---------|----------------|
| SkillBot API | `https://localhost:7101` |
| Web UI | `http://localhost:5000` |
| Swagger UI | `https://localhost:7101/swagger` |

### Quick Start (Web UI)

```bash
# Run the API first
dotnet run --project SkillBot.Api/SkillBot.Api.csproj

# In a second terminal, run the web UI
dotnet run --project SkillBot.Web/SkillBot.Web.csproj

# Open http://localhost:5000 in your browser
```

See **[docs/frontend/DEVELOPMENT.md](docs/frontend/DEVELOPMENT.md)** for full setup instructions and **[docs/USER_GUIDE_WEB.md](docs/USER_GUIDE_WEB.md)** for usage instructions.

---

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An API key for at least one LLM provider (OpenAI / Claude / Gemini)

### Manual Setup

```bash
# 1. Clone the repository
git clone https://github.com/harshil-sh/SkillBot.git
cd SkillBot

# 2. Restore packages
dotnet restore SkillBot.slnx

# 3. Set your API key (development)
cd SkillBot.Api
dotnet user-secrets set "SkillBot:ApiKey" "sk-your-openai-key"
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-min-32-chars-long"

# 4. Build and run the API
dotnet run --project SkillBot.Api/SkillBot.Api.csproj
# API: https://localhost:7101  |  Swagger: https://localhost:7101/
```

### Console Client

```bash
# Single-agent mode
dotnet run --project SkillBot.Console/SkillBot.Console.csproj

# Multi-agent mode
dotnet run --project SkillBot.Console/SkillBot.Console.csproj -- --multi-agent
```

### Docker (Coming Soon)

```bash
docker compose up
```

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  Clients                                                  │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │  Telegram   │  │  REST API    │  │  Console Client │ │
│  │    Bot      │  │  (JWT Auth)  │  │                 │ │
│  └──────┬──────┘  └──────┬───────┘  └────────┬────────┘ │
└─────────┼────────────────┼───────────────────┼──────────┘
          │                │                   │
          ▼                ▼                   ▼
┌─────────────────────────────────────────────────────────┐
│  SkillBot.Api  (ASP.NET Core)                            │
│  Controllers · JWT · Rate Limiting · Content Safety      │
│  Hangfire · EF Core SQLite · Serilog                     │
└────────────────────────┬────────────────────────────────┘
                         │
          ┌──────────────▼──────────────┐
          │  SkillBot.Infrastructure     │
          │  SemanticKernelEngine        │
          │  Multi-Agent Orchestrator    │
          │  LLM Providers (OAI/Claude/  │
          │    Gemini)                   │
          │  Two-Tier Cache              │
          └──────────────┬──────────────┘
                         │
          ┌──────────────▼──────────────┐
          │  SkillBot.Core               │
          │  Interfaces · Models         │
          │  Domain Contracts            │
          └─────────────────────────────┘
```

Projects:

| Project | Role |
|---|---|
| `SkillBot.Core` | Interfaces, domain models, no external dependencies |
| `SkillBot.Infrastructure` | Semantic Kernel, agents, cache, LLM providers, channels |
| `SkillBot.Api` | ASP.NET Core host, JWT auth, EF Core, Hangfire |
| `SkillBot.Console` | Interactive terminal client that calls the API |
| `SkillBot.Plugins` | Calculator, Weather, Time, SerpAPI search plugins |
| `SkillBot.Web` | Blazor WebAssembly SPA with MudBlazor 9.x |

---

## ⚙️ Configuration

Edit `SkillBot.Api/appsettings.json` (or use environment variables / `dotnet user-secrets`):

```json
{
  "ConnectionStrings": {
    "SkillBot": "Data Source=skillbot.db"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-key-min-32-chars-long",
    "Issuer": "SkillBot",
    "Audience": "SkillBotUsers",
    "ExpirationMinutes": 1440
  },
  "SkillBot": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4",
    "Caching": { "Enabled": true }
  },
  "SerpApi": {
    "ApiKey": "your-serpapi-key"
  },
  "Channels": {
    "Telegram": {
      "Enabled": false,
      "BotToken": "",
      "WebhookUrl": "https://yourdomain.com/api/webhook/telegram"
    }
  }
}
```

See **[docs/CONFIGURATION.md](docs/CONFIGURATION.md)** for all options.

---

## 📡 API Usage Examples

```bash
BASE=https://localhost:7101

# Register a user
curl -k -X POST $BASE/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","username":"alice","password":"Secret123!"}'

# Login and get JWT token
TOKEN=$(curl -k -s -X POST $BASE/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Secret123!"}' | jq -r .token)

# Send a chat message
curl -k -X POST $BASE/api/chat \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"What is 42 * 37?"}'

# Multi-agent task
curl -k -X POST $BASE/api/multi-agent/chat \
  -H "Content-Type: application/json" \
  -d '{"task":"Research quantum computing and write a summary"}'

# List available plugins
curl -k $BASE/api/plugins
```

See **[docs/API.md](docs/API.md)** for full endpoint reference.

---

## 📱 Telegram Bot

1. Create a bot with [@BotFather](https://t.me/BotFather) and copy the token
2. Set `Channels:Telegram:BotToken` and `Channels:Telegram:Enabled: true`
3. Expose your API via [ngrok](https://ngrok.com) in development: `ngrok http 7101`
4. Set `Channels:Telegram:WebhookUrl` to `https://<ngrok-subdomain>/api/webhook/telegram`

Available commands: `/start`, `/help`, `/settings`, `/setkey`, `/setprovider`, `/search`, `/multi`

See **[docs/TELEGRAM.md](docs/TELEGRAM.md)** for full setup guide.

---

## 🔌 Adding a Plugin

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SkillBot.Infrastructure.Plugins;

[Plugin(Name = "MyPlugin", Description = "Does something useful")]
public class MyPlugin
{
    [KernelFunction("my_function")]
    [Description("Transforms the input text")]
    public string Transform([Description("The text to transform")] string input)
        => input.ToUpperInvariant();
}

// Register in SkillBot.Api/Program.cs or SkillBot.Console/Program.cs
pluginProvider.RegisterPlugin(new MyPlugin());
```

See **[docs/PLUGIN-DEVELOPMENT.md](docs/PLUGIN-DEVELOPMENT.md)** for a full guide.

---

## 📚 Documentation

| Document | Description |
|---|---|
| [docs/INSTALLATION.md](docs/INSTALLATION.md) | Step-by-step installation guide |
| [docs/CONFIGURATION.md](docs/CONFIGURATION.md) | All configuration options |
| [docs/API.md](docs/API.md) | Full REST API reference |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Architecture deep-dive |
| [docs/TELEGRAM.md](docs/TELEGRAM.md) | Telegram bot setup |
| [docs/PLUGIN-DEVELOPMENT.md](docs/PLUGIN-DEVELOPMENT.md) | Writing custom plugins |
| [docs/CHANNELS.md](docs/CHANNELS.md) | Adding new messaging channels |
| [docs/USER_GUIDE_WEB.md](docs/USER_GUIDE_WEB.md) | Web UI user guide |
| [docs/ADMIN_GUIDE.md](docs/ADMIN_GUIDE.md) | Administrator guide |
| [docs/DEPLOYMENT_WEB.md](docs/DEPLOYMENT_WEB.md) | Web UI deployment (Docker/nginx/IIS) |
| [docs/TROUBLESHOOTING_WEB.md](docs/TROUBLESHOOTING_WEB.md) | Web UI troubleshooting |
| [docs/FAQ_WEB.md](docs/FAQ_WEB.md) | Frequently asked questions |
| [docs/ACCESSIBILITY.md](docs/ACCESSIBILITY.md) | WCAG 2.1 AA accessibility guide |
| [docs/frontend/ARCHITECTURE.md](docs/frontend/ARCHITECTURE.md) | Blazor frontend architecture |
| [docs/frontend/COMPONENTS.md](docs/frontend/COMPONENTS.md) | Component reference |
| [docs/frontend/STATE_MANAGEMENT.md](docs/frontend/STATE_MANAGEMENT.md) | State management patterns |
| [docs/frontend/API_INTEGRATION.md](docs/frontend/API_INTEGRATION.md) | API client reference |
| [docs/frontend/STYLING.md](docs/frontend/STYLING.md) | MudBlazor theme and CSS guide |
| [docs/frontend/DEVELOPMENT.md](docs/frontend/DEVELOPMENT.md) | Frontend developer setup |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Contributor guide |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

```bash
# Run integration tests
powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1

# Focused test scripts
powershell -File .\test-settings.ps1
powershell -File .\test-console.ps1
powershell -File .\test-database.ps1
```

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 🙏 Acknowledgments

- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- [Hangfire](https://www.hangfire.io/)
- [Serilog](https://serilog.net/)
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
