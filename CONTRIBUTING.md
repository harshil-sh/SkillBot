# Contributing to SkillBot

Thank you for your interest in contributing to SkillBot! 🎉

---

## Table of Contents

1. [Code of Conduct](#1-code-of-conduct)
2. [Setting Up the Dev Environment](#2-setting-up-the-dev-environment)
3. [Running the Tests](#3-running-the-tests)
4. [Code Style](#4-code-style)
5. [Adding New Channels](#5-adding-new-channels)
6. [Adding New Plugins](#6-adding-new-plugins)
7. [Pull Request Process](#7-pull-request-process)

---

## 1. Code of Conduct

We are committed to a welcoming, inclusive environment. Contributors are expected to:

- ✅ Be respectful and constructive in all interactions
- ✅ Welcome newcomers and help them learn
- ✅ Focus on technical merit, not personal preferences

Unacceptable behaviour: harassment, discriminatory language, personal attacks, or publishing others' private information.

---

## 2. Setting Up the Dev Environment

**Prerequisites:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- IDE of your choice (Visual Studio 2022, VS Code, or JetBrains Rider)
- (Optional) [DB Browser for SQLite](https://sqlitebrowser.org/) for inspecting databases
- (Optional) Docker for testing containerisation

```bash
# 1. Fork the repository on GitHub, then:
git clone https://github.com/YOUR_USERNAME/SkillBot.git
cd SkillBot

# 2. Add the upstream remote
git remote add upstream https://github.com/harshil-sh/SkillBot.git

# 3. Restore dependencies
dotnet restore SkillBot.slnx

# 4. Configure secrets
cd SkillBot.Api
dotnet user-secrets set "SkillBot:ApiKey" "sk-your-key"
dotnet user-secrets set "JwtSettings:Secret" "your-min-32-char-secret"

# 5. Build
dotnet build SkillBot.slnx

# 6. Run the API
dotnet run --project SkillBot.Api/SkillBot.Api.csproj

# 7. In another terminal, run the console
dotnet run --project SkillBot.Console/SkillBot.Console.csproj
```

---

## 3. Running the Tests

SkillBot's automated checks are PowerShell integration scripts (unit test projects are located in `SkillBot.Tests.Unit` and `SkillBot.Tests.Integration`).

```powershell
# Full feature test suite (requires a running API)
powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1

# Useful switches
powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1 -SkipBuild
powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1 -SkipConsole
powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1 -TestRateLimit

# Focused scripts
powershell -File .\test-settings.ps1
powershell -File .\test-console.ps1
powershell -File .\test-database.ps1

# .NET unit tests (when present)
dotnet test SkillBot.slnx
```

All tests must pass before submitting a PR.

---

## 4. Code Style

SkillBot follows standard .NET/C# conventions.

### Naming

```csharp
// Classes, interfaces, methods, properties: PascalCase
public class AgentEngine { }
public interface IAgentEngine { }
public void ExecuteAsync() { }
public string Name { get; init; }

// Private fields: _camelCase
private readonly ILogger _logger;

// Parameters, locals: camelCase
public void Process(string userId) { }

// Constants: PascalCase
public const int MaxRetries = 3;
```

### Immutable models

Prefer C# `record` types with `init`-only properties for domain models:

```csharp
public record User
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string PreferredProvider { get; init; } = "openai";
}

// Update with 'with' expressions
var updated = user with { PreferredProvider = "claude" };
```

### Nullable reference types

All projects have `<Nullable>enable</Nullable>`. Always annotate nullable types explicitly:

```csharp
public string? GetOptionalValue() => null;
public void Process(string? input)
{
    if (input is null) return;
    // ...
}
```

### Async/await

Never use `.Result` or `.Wait()`. Always `await`:

```csharp
// Good
var result = await service.GetAsync(id);

// Bad
var result = service.GetAsync(id).Result;
```

### Error handling

Handle expected errors gracefully; log and propagate unexpected ones:

```csharp
try
{
    return await _client.GetStringAsync(url);
}
catch (HttpRequestException ex)
{
    _logger.LogWarning(ex, "Failed to fetch {Url}", url);
    return null;
}
```

### Controllers stay thin

Validation, safety checks, rate limiting, and persistence logic live in services or repositories. Controllers only orchestrate calls and map results to HTTP responses.

### Structured logging

Use structured templates, not string interpolation:

```csharp
// Good
_logger.LogInformation("User {UserId} sent {Tokens} tokens", userId, tokens);

// Avoid
_logger.LogInformation($"User {userId} sent {tokens} tokens");
```

---

## 5. Adding New Channels

A "channel" is any external messaging platform (Telegram, WhatsApp, Slack, Discord, etc.).

### Quick summary

1. Implement `IMessagingChannel` (or subclass `BaseMessagingChannel` for built-in user mapping).
2. Add a `Channels:MyChannel` section to `appsettings.json`.
3. Register the channel service in `Program.cs` (conditioned on `Enabled`).
4. Register with `IChannelManager` after `builder.Build()`.
5. Add a webhook controller inheriting `WebhookControllerBase`.

### `IMessagingChannel` interface

```csharp
public interface IMessagingChannel
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<bool> SendMessageAsync(string userId, string message);
    Task<Message?> ReceiveMessageAsync();
    Task<bool> RegisterUserAsync(string channelUserId, string systemUserId);
    Task<User?> GetUserByChannelIdAsync(string channelUserId);
}
```

For the full walkthrough with code examples for WhatsApp, Slack, and Discord, see [docs/CHANNELS.md](docs/CHANNELS.md).

---

## 6. Adding New Plugins

Plugins extend what the AI agent can do (call APIs, perform calculations, search the web, etc.).

### Plugin structure

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SkillBot.Infrastructure.Plugins; // for [Plugin]

[Plugin(Name = "MyPlugin", Description = "Does something useful for the AI agent")]
public class MyPlugin
{
    [KernelFunction("my_function")]
    [Description("Transforms the input text to upper case")]
    public string Transform(
        [Description("The text to transform")] string input)
        => input.ToUpperInvariant();

    [KernelFunction("fetch_data")]
    [Description("Fetches data from an external API")]
    public async Task<string> FetchDataAsync(
        [Description("The query to look up")] string query,
        CancellationToken cancellationToken = default)
    {
        // async operations are supported
        await Task.Delay(10, cancellationToken);
        return $"Data for: {query}";
    }
}
```

**Attribute checklist:**

| Attribute | Required | Notes |
|---|---|---|
| `[Plugin(Name, Description)]` | ✅ | Class-level. Description is shown to the LLM. |
| `[KernelFunction("name")]` | ✅ | Method-level. Name must be snake_case. |
| `[Description("...")]` | ✅ | Method-level AND each parameter. Clear descriptions improve LLM tool selection. |

### Register the plugin

**API** (`SkillBot.Api/Program.cs`, inside the plugin registration block):

```csharp
pluginProvider.RegisterPlugin(new MyPlugin());
```

**Console** (`SkillBot.Console/Program.cs`):

```csharp
pluginProvider.RegisterPlugin(new MyPlugin());
```

If the plugin requires dependencies (configuration, HTTP clients, etc.), resolve them from the `IServiceProvider` before instantiating:

```csharp
var myService = scope.ServiceProvider.GetRequiredService<IMyService>();
pluginProvider.RegisterPlugin(new MyPlugin(myService));
```

### Plugin best practices

- **Focused**: One concern per plugin.
- **Documented**: Write clear `[Description]` values — the LLM uses them to decide when to call your plugin.
- **Async-safe**: Use `CancellationToken` on async methods.
- **Error-resilient**: Return a helpful string on failure rather than throwing, so the LLM can relay the error to the user.
- **Fast**: Aim for < 5 seconds per call; long-running operations should be offloaded to background tasks.

For a full guide with advanced patterns (DI, caching, external APIs), see [docs/PLUGIN-DEVELOPMENT.md](docs/PLUGIN-DEVELOPMENT.md).

---

## 7. Pull Request Process

### Branch naming

```
feature/add-github-plugin
fix/sqlite-lock-on-concurrent-writes
docs/improve-telegram-setup-guide
refactor/extract-cache-service
```

### Before opening a PR

- [ ] `dotnet build SkillBot.slnx` — no warnings or errors
- [ ] `dotnet test SkillBot.slnx` — all tests pass
- [ ] `powershell -File .\scripts\Test-SkillBot-AllFeatures.ps1 -SkipBuild` — integration tests pass
- [ ] Code follows the style guidelines in section 4
- [ ] Public APIs have XML doc comments (`/// <summary>`)
- [ ] Relevant documentation updated (this file, `docs/`, `CHANGELOG.md`)
- [ ] No secrets or API keys in the diff

### PR description template

```markdown
## What does this PR do?
Brief description of the change.

## Type of change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## How was it tested?
Describe the testing you performed.

## Checklist
- [ ] Build passes
- [ ] Tests pass
- [ ] Docs updated
- [ ] No secrets in diff
```

### Review process

1. CI checks run automatically.
2. At least one maintainer reviews the code.
3. Address review feedback; request a re-review when ready.
4. Maintainer squash-merges the PR.

---

**Questions?** Open a GitHub Discussion or Issue. We are happy to help! 🚀
