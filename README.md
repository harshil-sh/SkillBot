<div align="center">

# 🤖 SkillBot

### Your AI assistant. Your server. Your rules.

[![CI](https://github.com/harshil-sh/skillbot/actions/workflows/ci.yml/badge.svg)](https://github.com/harshil-sh/skillbot/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/harshil-sh/skillbot/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Blazor](https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](https://github.com/harshil-sh/skillbot/blob/main/Dockerfile)
[![GitHub Stars](https://img.shields.io/github/stars/harshil-sh/skillbot?style=social)](https://github.com/harshil-sh/skillbot/stargazers)

**SkillBot** is a production-ready, self-hosted AI assistant powered by .NET 10 and Microsoft Semantic Kernel.  
Chat with OpenAI, Claude, or Gemini through a beautiful web UI, Telegram bot, REST API, or terminal console.  
Everything runs on your own infrastructure — your data never leaves your server.

[**Quick Start**](#-quick-start) · [**Features**](#-features) · [**Docs**](#-documentation) · [**Roadmap**](#️-roadmap) · [**Contributing**](#-contributing)

> 📸 **Screenshots coming soon** — the web UI is fully functional; visual walkthroughs are being prepared.

</div>

---

## 💡 Why SkillBot?

Most AI assistant tools are cloud-only, lock you into a single provider, and send your conversations to someone else's servers. SkillBot takes a different approach:

| | SkillBot |
|---|---|
| 🔒 **Privacy-first** | Runs entirely on your own server. Conversations stay in your SQLite database, never leaving your infrastructure. |
| 🔄 **Provider-flexible** | Switch between OpenAI GPT-4, Anthropic Claude, and Google Gemini per-user at runtime — no redeploy needed. |
| 🌐 **Every interface covered** | One deployment gives you a polished web UI, a Telegram bot, a REST API, and an interactive console. |
| 🤖 **Multi-agent intelligence** | Route complex tasks to specialised agents (Researcher, Coder, Data Analyst, Writer) that collaborate automatically. |
| 🛠️ **Extensible by design** | Add capabilities in minutes with the plugin system — just drop in a class with `[Plugin]` and `[KernelFunction]` attributes. |

---

## ✨ Features

### 🧠 AI & Language Models
- **Multi-LLM support** — Use OpenAI (GPT-4/4o), Anthropic Claude, or Google Gemini; switch provider per-user at runtime without restarting
- **Microsoft Semantic Kernel** — Battle-tested orchestration layer with memory, planning, and function calling
- **Multi-agent orchestration** — Automatically routes tasks to specialised agents: Research, Coding, Data Analysis, and Writing, with single/parallel/sequential strategies
- **Web search integration** — SerpAPI-powered real-time search with two-tier caching (in-memory + SQLite)

### 🖥️ Interfaces
- **Blazor WebAssembly UI** — Modern SPA with MudBlazor Material Design: chat, settings, admin dashboard, dark mode, keyboard shortcuts, and full mobile support
- **Telegram bot** — Full-featured bot with `/start`, `/help`, `/setkey`, `/setprovider`, `/search`, `/multi`, and per-user API key management
- **REST API** — ASP.NET Core with JWT authentication, Swagger UI, rate limiting, and content safety middleware
- **Console client** — Interactive terminal interface with single-agent and multi-agent modes

### 🔌 Platform & Infrastructure
- **Plugin system** — Reflection-based discovery using `[Plugin]`, `[KernelFunction]`, and `[Description]` attributes; built-in plugins for Calculator, Weather, Time, and Web Search
- **Two-tier caching** — L1 in-memory + L2 SQLite cache for LLM responses keyed by SHA-256 content hash
- **SQLite persistence** — Conversation history and user settings via EF Core; zero configuration, file-based
- **Background task scheduling** — Hangfire-powered one-time and recurring jobs; cache cleanup, usage reports
- **Token usage tracking** — Per-user, per-conversation token consumption statistics

### 🛡️ Security
- **JWT authentication** — Configurable expiry, issuer, and audience; per-user API key storage (encrypted at rest)
- **Rate limiting** — Per-user and per-endpoint request throttling, configurable via appsettings
- **Input validation** — Length limits, content safety checks, and prompt-injection detection
- **Structured logging** — Serilog with rolling file and console sinks; configurable log levels per namespace

---

## ⚡ Quick Start

### Option 1 — Docker (Recommended)

```bash
# Clone the repository
git clone https://github.com/harshil-sh/skillbot.git
cd skillbot

# Copy the example environment file and fill in your keys
cp .env.example .env

# Edit .env with your API key and a JWT secret (min 32 characters)
# OPENAI_API_KEY=sk-...
# JWT_SECRET=your-super-secret-key-at-least-32-chars

# Start everything (API + Web UI + background jobs)
docker compose up -d

# The API is available at http://localhost:8080
# The web UI is served by the API at http://localhost:8080
```

That's it. Open `http://localhost:8080`, register an account, and start chatting.

### Option 2 — Manual (.NET SDK)

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) · An API key for OpenAI, Claude, or Gemini

```bash
# Clone and restore
git clone https://github.com/harshil-sh/skillbot.git
cd skillbot
dotnet restore SkillBot.slnx

# Set secrets (development only)
cd SkillBot.Api
dotnet user-secrets set "SkillBot:ApiKey"     "sk-your-openai-key"
dotnet user-secrets set "JwtSettings:Secret"  "your-super-secret-jwt-key-min-32-chars"
cd ..

# Apply the database migration and start
dotnet run --project SkillBot.Api/SkillBot.Api.csproj
```

| Service | URL |
|---------|-----|
| REST API | `https://localhost:7101` |
| Swagger UI | `https://localhost:7101/swagger` |
| Web UI (dev) | `http://localhost:5000` |

```bash
# (Optional) Run the web UI separately in development
dotnet run --project SkillBot.Web/SkillBot.Web.csproj

# (Optional) Launch the console client
dotnet run --project SkillBot.Console/SkillBot.Console.csproj
dotnet run --project SkillBot.Console/SkillBot.Console.csproj -- --multi-agent
```

### Option 3 — Setup Script

```bash
# Linux / macOS
chmod +x scripts/setup.sh && ./scripts/setup.sh

# Windows (PowerShell)
.\scripts\setup.ps1
```

The script checks prerequisites, generates a JWT secret, guides you through the `.env` configuration, and starts the application with Docker Compose.

---

## 🏗️ Architecture

SkillBot follows a clean layered architecture with clear dependency rules:

```
┌──────────────────────────────────────────────────────────┐
│  Clients                                                  │
│  ┌────────────┐  ┌─────────────┐  ┌──────────────────┐  │
│  │ Web UI     │  │ Telegram    │  │ Console Client   │  │
│  │ (Blazor    │  │ Bot         │  │ (API client)     │  │
│  │  WASM)     │  │             │  │                  │  │
│  └─────┬──────┘  └──────┬──────┘  └────────┬─────────┘  │
└────────┼────────────────┼──────────────────┼────────────┘
         │                │                  │
         ▼                ▼                  ▼
┌──────────────────────────────────────────────────────────┐
│  SkillBot.Api  (ASP.NET Core)                            │
│  Auth · Rate Limiting · Content Safety · Hangfire        │
│  EF Core → SQLite · Serilog · Swagger                    │
└──────────────────────┬───────────────────────────────────┘
                       │
         ┌─────────────▼─────────────┐
         │  SkillBot.Infrastructure  │
         │  SemanticKernelEngine     │
         │  LlmTaskRouter            │
         │  AgentOrchestrator        │
         │  LLM Providers            │
         │  Two-Tier Cache           │
         │  Channel Manager          │
         └─────────────┬─────────────┘
                       │
         ┌─────────────▼─────────────┐
         │  SkillBot.Core            │
         │  Interfaces · Models      │
         │  Domain Contracts         │
         └───────────────────────────┘
```

| Project | Responsibility |
|---------|---------------|
| `SkillBot.Core` | Interfaces, domain models, shared contracts — zero external dependencies |
| `SkillBot.Infrastructure` | Semantic Kernel, agent orchestration, LLM providers, channels, cache |
| `SkillBot.Api` | ASP.NET Core host, JWT auth, EF Core, Hangfire background jobs |
| `SkillBot.Console` | Authenticated terminal client that calls the API |
| `SkillBot.Plugins` | Calculator, Weather, Time, and SerpAPI search plugins |
| `SkillBot.Web` | Blazor WebAssembly SPA (MudBlazor 9.x) |
| `SkillBot.Tests.Unit` | 50 unit tests — services, LLM factory, channel manager |
| `SkillBot.Tests.Integration` | 15 integration tests — auth, chat, settings, Telegram webhook |
| `SkillBot.Tests.E2E` | Playwright end-to-end tests (require live server, marked `[Explicit]`) |

---

## 🆚 SkillBot vs ChatGPT / Other Tools

| Feature | SkillBot | ChatGPT | Ollama | LiteLLM |
|---------|----------|---------|--------|---------|
| **Self-hosted** | ✅ | ❌ | ✅ | ✅ |
| **Your data stays local** | ✅ | ❌ | ✅ | Varies |
| **OpenAI support** | ✅ | ✅ | ❌ | ✅ |
| **Claude support** | ✅ | ❌ | ❌ | ✅ |
| **Gemini support** | ✅ | ❌ | ❌ | ✅ |
| **Telegram bot** | ✅ | ❌ | ❌ | ❌ |
| **Web UI included** | ✅ | ✅ | ✅ | ❌ |
| **REST API** | ✅ | API only | ✅ | ✅ |
| **Console client** | ✅ | ❌ | ✅ | ❌ |
| **Multi-agent orchestration** | ✅ | ❌ | ❌ | ❌ |
| **Plugin system** | ✅ | ✅ (GPTs) | ❌ | ❌ |
| **Per-user API keys** | ✅ | N/A | ❌ | ❌ |
| **Web search** | ✅ | ✅ (Plus) | ❌ | ❌ |
| **Docker deployment** | ✅ | N/A | ✅ | ✅ |
| **Free / open source** | ✅ MIT | ❌ | ✅ MIT | ✅ MIT |

---

## ⚙️ Configuration

The most important settings in `SkillBot.Api/appsettings.json`:

```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-min-32-chars-long",
    "ExpirationMinutes": 1440
  },
  "SkillBot": {
    "ApiKey":  "sk-your-openai-key",
    "Model":   "gpt-4",
    "Caching": { "Enabled": true }
  },
  "SerpApi": {
    "ApiKey": "your-serpapi-key"
  },
  "Channels": {
    "Telegram": {
      "Enabled":    false,
      "BotToken":   "",
      "BotUsername": "",
      "WebhookUrl": "https://yourdomain.com/api/webhook/telegram"
    }
  }
}
```

Every value can be overridden with environment variables (e.g. `SkillBot__ApiKey=sk-...`).  
See **[docs/CONFIGURATION.md](docs/CONFIGURATION.md)** for the complete reference.

---

## 📚 Documentation

### User Guides
| Document | Description |
|----------|-------------|
| [docs/INSTALLATION.md](docs/INSTALLATION.md) | Prerequisites, secrets setup, first run |
| [docs/CONFIGURATION.md](docs/CONFIGURATION.md) | Every `appsettings.json` key documented |
| [docs/USER_GUIDE_WEB.md](docs/USER_GUIDE_WEB.md) | Web UI walkthrough |
| [docs/ADMIN_GUIDE.md](docs/ADMIN_GUIDE.md) | Admin dashboard and user management |
| [docs/TELEGRAM.md](docs/TELEGRAM.md) | BotFather setup, webhook config, commands |
| [docs/FAQ_WEB.md](docs/FAQ_WEB.md) | Frequently asked questions |

### API & Deployment
| Document | Description |
|----------|-------------|
| [docs/API.md](docs/API.md) | Full REST endpoint reference with `curl` examples |
| [docs/DEPLOYMENT_WEB.md](docs/DEPLOYMENT_WEB.md) | Docker, nginx, GitHub Pages, Azure, IIS |
| [docs/TROUBLESHOOTING_WEB.md](docs/TROUBLESHOOTING_WEB.md) | Common problems and solutions |
| [docs/RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md) | Pre-release checklist |

### Developer Docs
| Document | Description |
|----------|-------------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture deep-dive |
| [docs/frontend/ARCHITECTURE.md](docs/frontend/ARCHITECTURE.md) | Blazor frontend architecture |
| [docs/frontend/COMPONENTS.md](docs/frontend/COMPONENTS.md) | Component reference |
| [docs/frontend/API_INTEGRATION.md](docs/frontend/API_INTEGRATION.md) | Frontend API client |
| [docs/frontend/STYLING.md](docs/frontend/STYLING.md) | MudBlazor theme and CSS guide |
| [docs/frontend/DEVELOPMENT.md](docs/frontend/DEVELOPMENT.md) | Frontend dev environment setup |
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to contribute |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

---

## 🗺️ Roadmap

### v1.0 — Foundation ✅
- [x] Multi-LLM support (OpenAI, Claude, Gemini)
- [x] REST API with JWT authentication
- [x] Telegram bot with slash commands
- [x] Console client (single-agent and multi-agent)
- [x] Plugin system with built-in plugins
- [x] Two-tier caching (memory + SQLite)
- [x] Rate limiting and content safety
- [x] Blazor WebAssembly web UI
- [x] Admin dashboard
- [x] Docker deployment
- [x] CI/CD pipeline
- [x] Full test suite (unit + integration + E2E)

### v1.1 — User Experience 🚧
- [ ] Streaming responses (Server-Sent Events)
- [ ] Conversation export (JSON, Markdown, PDF)
- [ ] Voice input support (Web Speech API)
- [ ] File upload and document Q&A
- [ ] Conversation sharing (public links)
- [ ] Custom system prompts per conversation

### v1.2 — Integrations
- [ ] Discord bot channel
- [ ] Slack bot channel
- [ ] OpenRouter API support (access 50+ models)
- [ ] Local model support via Ollama
- [ ] Webhook outbound notifications
- [ ] Google Calendar / GitHub / Jira plugins

### v1.3 — Enterprise
- [ ] Team workspaces and shared conversations
- [ ] LDAP / SSO authentication
- [ ] Audit logging
- [ ] Kubernetes Helm chart
- [ ] Usage quotas and billing tracking
- [ ] Conversation analytics dashboard

---

## 🤝 Contributing

Contributions of all kinds are welcome — bug fixes, features, documentation, tests, and design feedback.

```bash
# Fork and clone
git clone https://github.com/<your-username>/skillbot.git
cd skillbot

# Create a feature branch
git checkout -b feature/my-feature

# Build and test
dotnet build SkillBot.slnx
dotnet test SkillBot.Tests.Unit
dotnet test SkillBot.Tests.Integration

# Run the full integration test suite
powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1 -SkipBuild

# Push and open a PR
git push origin feature/my-feature
```

Please read **[CONTRIBUTING.md](CONTRIBUTING.md)** before submitting a pull request — it covers code style, commit message format, and the review process.

---

## 🆘 Support

| Channel | Link |
|---------|------|
| 🐛 Bug reports | [GitHub Issues](https://github.com/harshil-sh/skillbot/issues/new?template=bug_report.md) |
| 💡 Feature requests | [GitHub Discussions](https://github.com/harshil-sh/skillbot/discussions/new?category=ideas) |
| ❓ Questions | [GitHub Discussions — Q&A](https://github.com/harshil-sh/skillbot/discussions/new?category=q-a) |
| 📧 Direct contact | [Harshil.sh@gmail.com](mailto:Harshil.sh@gmail.com) |

If SkillBot is useful to you, please consider giving it a ⭐ on GitHub — it genuinely helps with visibility.

---

## 📄 License

```
MIT License

Copyright (c) 2026 Harshil Shah

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 Acknowledgements

SkillBot is built on the shoulders of some excellent open-source projects:

- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) — AI orchestration framework
- [MudBlazor](https://mudblazor.com) — Blazor component library
- [Hangfire](https://www.hangfire.io) — Background job processing
- [Serilog](https://serilog.net) — Structured logging
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) — Telegram Bot API client
- [Markdig](https://github.com/xoofx/markdig) — Markdown processor for .NET
- [Blazored.LocalStorage](https://github.com/Blazored/LocalStorage) — localStorage for Blazor

---

<div align="center">

Made with ❤️ by [Harshil Shah](https://github.com/harshil-sh)

</div>
