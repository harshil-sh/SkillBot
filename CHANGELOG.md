# Changelog

All notable changes to SkillBot are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
SkillBot adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned
- Docker support (`Dockerfile` + `docker-compose.yml`)
- Web front-end (Blazor or React)
- RAG / vector database integration
- Persistent Hangfire storage
- Streaming chat responses (Server-Sent Events)
- WhatsApp channel (Twilio)

---

## [1.0.0] — Initial Release

### Added

#### Multi-LLM Support
- OpenAI GPT-4 as the default provider
- Anthropic Claude integration via `LLMProviderFactory`
- Google Gemini integration via `LLMProviderFactory`
- Per-user provider switching: users can set a preferred provider and store their own API keys via `PUT /api/settings/provider` and `PUT /api/settings/api-key`
- Fallback to shared system API key when no per-user key is set

#### REST API (ASP.NET Core)
- `POST /api/auth/register` — user registration with password hashing
- `POST /api/auth/login` — JWT issuance
- `POST /api/chat` — single-agent chat with caching, rate limiting, input validation, and content safety
- `GET /api/chat/history` — authenticated user's conversation history
- `GET /api/chat/{id}` — retrieve conversation messages
- `DELETE /api/chat/{id}` — delete a conversation
- `POST /api/multi-agent/chat` — multi-agent orchestration endpoint
- `GET /api/multi-agent/agents` — list available specialised agents
- `GET /api/settings` — read user settings (providers, key presence flags)
- `PUT /api/settings/api-key` — store per-provider API key
- `PUT /api/settings/provider` — switch preferred LLM provider
- `GET /api/plugins` — list registered plugins and their schemas
- `GET /api/plugins/{name}` — plugin detail
- `GET /api/usage/stats` — overall token usage statistics
- `GET /api/usage/stats/{conversationId}` — per-conversation stats
- `GET /api/usage/top-conversations` — top conversations by token usage
- `DELETE /api/usage/stats` — reset statistics
- `POST /api/tasks/schedule` — schedule a one-time agent task
- `POST /api/tasks/recurring` — schedule a recurring task with a cron expression
- `GET /api/tasks` / `GET /api/tasks/{id}` / `DELETE /api/tasks/{id}` — task management
- `GET /api/cache/stats` — two-tier cache statistics
- `GET /api/cache/health` — cache health status
- `DELETE /api/cache` — clear all cache entries
- `DELETE /api/cache/invalidate/{pattern}` — pattern-based cache invalidation
- `POST /api/webhook/telegram` — Telegram webhook receiver
- `GET /health` — health check endpoint
- Swagger UI available at `/` in development

#### Telegram Bot Integration
- System-level single bot via `Channels:Telegram` configuration
- Webhook-based message processing via `TelegramWebhookController`
- Bot commands: `/start`, `/help`, `/settings`, `/setkey`, `/setprovider`, `/search`, `/multi`
- Per-user API key and provider management from within Telegram
- Long message auto-splitting (4000-character chunks)
- Automatic channel user registration on first message

#### Console Client
- Interactive terminal interface connecting to the API
- Single-agent and multi-agent modes (`--multi-agent` flag)
- Commands: chat (authenticated users), auth, settings, search, admin flows
- Auth tokens and preferences persisted to `%APPDATA%\SkillBot\console-settings.json`

#### Multi-Agent Orchestration
- `LlmTaskRouter` — LLM-powered routing that selects strategy and agents
- `AgentOrchestrator` — coordinates `single`, `parallel`, and `sequential` strategies
- `ResearchAgent` — research, fact gathering, information synthesis
- `CodingAgent` — code generation, debugging, code review
- `DataAnalysisAgent` — data interpretation, statistics, trend analysis
- `WritingAgent` — writing, editing, summarisation, content generation

#### Plugin System
- `DynamicPluginProvider` — reflection-based plugin discovery and registration
- `[Plugin]`, `[KernelFunction]`, `[Description]` attribute convention
- Built-in plugins: `CalculatorPlugin`, `WeatherPlugin`, `TimePlugin`, `SimpleUsagePlugin`
- `SerpApiPlugin` — web, news, and image search via [SerpAPI](https://serpapi.com/)
- `CachedSerpApiPlugin` — SerpAPI wrapped with two-tier cache

#### Two-Tier Caching
- L1: `IMemoryCache` (in-process, configurable size limit)
- L2: SQLite persistent cache (`skillbot-cache.db`)
- SHA-256-based cache keys for LLM responses
- Per-type TTL configuration (web search 1 h, news 15 min, agent responses 12 h, etc.)
- Cache management REST API
- Background auto-cleanup via Hangfire

#### Security
- JWT Bearer authentication with configurable secret, issuer, audience, and expiry
- `InputValidator` — length and pattern checks (injection prevention)
- `ContentSafetyService` — keyword/pattern-based content policy enforcement
- `RateLimiter` — per-user, per-endpoint sliding window rate limiting
- `SecurityMiddleware` — security response headers (CSP, X-Frame-Options, HSTS, etc.)
- `ErrorHandlingMiddleware` — catch-all with structured error responses
- `RequestLoggingMiddleware` — structured request/response logging

#### Background Tasks
- Hangfire integration with in-memory storage
- One-time task scheduling (`ScheduleAgentTask`)
- Recurring task scheduling with cron expressions (`ScheduleRecurringTask`)
- Hangfire Dashboard at `/hangfire`

#### Persistence (SQLite + EF Core)
- `SkillBotDbContext` with `Users`, `Conversations`, `ChannelUsers` tables
- `DbInitializer` — automatic schema creation on first run
- `SqliteUserRepository`, `ConversationRepository`, `ChannelUserRepository`
- `SqliteMemoryProvider` — conversation memory for the Semantic Kernel engine

#### Logging & Observability
- Serilog with console and daily rolling file sinks
- Structured log templates throughout
- `GET /health` health check for the agent engine

#### Architecture
- Clean architecture with `Core` → `Infrastructure` → `Plugins` → `Api`/`Console` dependency flow
- Channel abstraction pattern (`IMessagingChannel`, `BaseMessagingChannel`, `IChannelManager`)
- `WebhookHandlerService` — unified inbound message processing pipeline
- Full async/await throughout; no blocking calls
- C# 12 records with `with` expressions for immutable model updates
- .NET 10 target framework

[Unreleased]: https://github.com/harshil-sh/SkillBot/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/harshil-sh/SkillBot/releases/tag/v1.0.0
