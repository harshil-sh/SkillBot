# SkillBot Web UI — Frequently Asked Questions

---

## Table of Contents

- [General](#general)
- [Usage](#usage)
- [Technical](#technical)
- [Account & Privacy](#account--privacy)

---

## General

### What is SkillBot?

SkillBot is a **self-hosted AI assistant** — you run it on your own server and connect it to the LLM providers of your choice (OpenAI, Anthropic Claude, or Google Gemini). Because it is self-hosted, you control your data: conversations are stored in your own SQLite database and never sent to third-party analytics services.

The web interface (`SkillBot.Web`) is a Blazor WebAssembly single-page application that provides a browser-based chat experience, settings management, and an admin dashboard.

### Is SkillBot free?

SkillBot itself is **free and open source** (MIT License). You do pay for the LLM provider API calls — costs depend on the provider and model you use:

| Provider | Free tier? | Paid tier |
|----------|-----------|-----------|
| OpenAI | No | ~$0.01–0.06 per 1K tokens (GPT-4) |
| Anthropic Claude | No | ~$0.003–0.015 per 1K tokens |
| Google Gemini | Yes (limited) | Pay-as-you-go |

### Which LLM providers are supported?

SkillBot supports three providers out of the box:

- **OpenAI** — GPT-4, GPT-4o, GPT-3.5-turbo
- **Anthropic Claude** — Claude 3 Opus, Sonnet, Haiku
- **Google Gemini** — Gemini 1.5 Pro, Flash

You can set a different provider per user. Administrators can configure a system-wide fallback provider.

### Does SkillBot have a mobile app?

Not currently. The Blazor WebAssembly web UI is fully responsive and works well on mobile browsers. It is also a Progressive Web App (PWA) — you can install it to your home screen from a supported mobile browser.

### Is there a Telegram bot?

Yes! SkillBot includes a built-in Telegram bot integration. Ask your administrator to enable it. Once enabled, you can use `/start` to begin chatting in Telegram with the same account.

---

## Usage

### How do I start a conversation?

1. Log in at your SkillBot URL.
2. You are taken to the Chat page automatically.
3. Type your message in the input box at the bottom and press **Enter**.

### Can I have multiple conversations?

Yes. Each conversation is saved separately in the sidebar on the left. Click **New Chat** (✏️) to start a new one, or click any existing conversation to continue it.

### What is Multi-Agent Mode?

Multi-agent mode routes your task to a team of specialised AI agents:

- **Research Agent** — gathers and synthesises information
- **Coding Agent** — writes and reviews code
- **Data Analysis Agent** — interprets data and statistics
- **Writing Agent** — drafts, edits, and formats text

The router (powered by an LLM) automatically selects the best strategy (single agent, parallel, or sequential) based on your task. Enable it with the **Multi-Agent** toggle in the chat toolbar.

### Can SkillBot search the web?

Yes, if your administrator has configured a SerpAPI key. SkillBot uses the SerpAPI plugin to fetch real-time search results. You can trigger it explicitly with `/search your query` or let SkillBot decide when a web search would be helpful.

### Does SkillBot remember previous conversations?

Within a conversation, SkillBot maintains context (the conversation history is sent with each message). Across different conversations, SkillBot starts fresh. If you want SkillBot to remember information across sessions, include that information in your **System Prompt** in Settings.

### How do I format code in my messages?

Wrap inline code in backticks: `` `code here` ``

For multi-line code blocks, use triple backticks with an optional language hint:

````
```python
def hello():
    print("Hello, world!")
```
````

SkillBot's assistant replies also support Markdown formatting and will render code blocks with syntax highlighting.

---

## Technical

### What technology does SkillBot use?

**Backend (SkillBot.Api):**
- .NET 10 / ASP.NET Core
- Microsoft Semantic Kernel (LLM orchestration)
- EF Core + SQLite (persistence)
- JWT authentication
- Hangfire (background tasks)
- Serilog (logging)

**Frontend (SkillBot.Web):**
- Blazor WebAssembly (.NET 10)
- MudBlazor 9.x (UI component library)
- Custom JWT auth state provider

**Plugins:**
- Calculator, Time, Weather (built-in)
- SerpAPI web search (optional)

### What browsers are supported?

Any modern browser with WebAssembly support:
- Chrome / Edge 90+
- Firefox 90+
- Safari 15+
- Chrome Android / Safari iOS 15.4+

Internet Explorer is not supported.

### How big is the app download?

On first load, the browser downloads:
- Blazor WASM runtime: ~2.5 MB (Brotli compressed)
- App code: ~300 KB compressed
- MudBlazor CSS: ~150 KB compressed

Total first load: ~3 MB compressed. Subsequent loads use the service worker cache and are near-instant.

### Is my data safe?

Your conversation data is stored in a SQLite database on the server you (or your organisation) controls. SkillBot does not send your data to any analytics or telemetry service. The only external calls are:
- LLM provider APIs (OpenAI / Claude / Gemini) — your messages are sent to the provider for inference
- SerpAPI (if configured) — your search queries are sent to SerpAPI

Ensure your SkillBot deployment uses HTTPS to protect data in transit.

### Can I self-host SkillBot?

Absolutely — that is the primary use case. See [docs/INSTALLATION.md](INSTALLATION.md) for full setup instructions and [docs/DEPLOYMENT_WEB.md](DEPLOYMENT_WEB.md) for web UI deployment options (Docker, nginx, IIS, Azure, Netlify).

### How do I add a new LLM provider?

SkillBot uses Microsoft Semantic Kernel, which supports any provider with an OpenAI-compatible API. To add a custom provider, implement a new `IChatCompletionService` in `SkillBot.Infrastructure` and register it in `Program.cs`. See [docs/CONFIGURATION.md](CONFIGURATION.md) for details.

---

## Account & Privacy

### How do I change my password?

1. Go to **Profile** in the navigation bar.
2. Scroll to the **Security** section.
3. Click **Change Password**.
4. Enter your current password and your new password (min 8 characters).
5. Click **Update Password**.

### How do I delete my account?

1. Go to **Profile** in the navigation bar.
2. Scroll to the **Danger Zone** section.
3. Click **Delete Account**.
4. Type your username in the confirmation dialog.
5. Click **Permanently Delete**.

This action is irreversible. All your conversations, settings, and API keys are deleted.

### Where are my API keys stored?

Your personal API keys (OpenAI, Claude, Gemini) are stored **encrypted on the server**. They are never returned to the browser in plain text — the settings page shows only the last 4 characters (e.g., `sk-...1234`).

### Who can see my conversations?

Only you can access your conversations through the web UI. Administrators with direct database access can technically read raw data, but the SkillBot admin UI does not expose conversation content. Ensure you trust your SkillBot administrator.

### Can I export my conversations?

There is no built-in export feature in the web UI at this time. As an administrator you can export directly from the SQLite database. If you need this feature, check the [GitHub issues](https://github.com/harshil-sh/SkillBot/issues) or open a feature request.

### How long are my conversations kept?

By default, conversations are kept indefinitely ("Forever"). You can change this in **Settings → Privacy → History Retention Period**:
- 7 days
- 30 days
- 1 year
- Forever (default)

The cleanup runs as a nightly Hangfire background job.

---

*See also: [USER_GUIDE_WEB.md](USER_GUIDE_WEB.md) · [TROUBLESHOOTING_WEB.md](TROUBLESHOOTING_WEB.md) · [ADMIN_GUIDE.md](ADMIN_GUIDE.md)*
