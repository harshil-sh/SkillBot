# SkillBot Codebase Context

**PRIVATE DOCUMENT - FOR AI CODING AGENTS ONLY**  
**Last Updated**: 2026-04-16

This document provides comprehensive context about the SkillBot codebase for AI coding assistants (Vibe, Cursor, GitHub Copilot, etc.). It explains the "why" behind architectural decisions, common patterns, gotchas, and development workflows.

## Table of Contents
- [Project Overview](#project-overview)
- [Architecture Philosophy](#architecture-philosophy)
- [Key Design Decisions](#key-design-decisions)
- [Common Patterns](#common-patterns)
- [File Organization](#file-organization)
- [Development Workflows](#development-workflows)
- [Testing Strategy](#testing-strategy)
- [Known Issues & Gotchas](#known-issues--gotchas)
- [Future Considerations](#future-considerations)

---

## Project Overview

### What is SkillBot?

SkillBot is an AI agent framework that allows:
1. **Single agent mode**: Traditional chatbot with tools/plugins
2. **Multi-agent mode**: Multiple specialized AI agents collaborating
3. **Plugin extensibility**: Easy-to-add tools via C# attributes
4. **Persistent memory**: SQLite-backed conversation history

### Core Value Proposition

Unlike frameworks that require complex configuration or custom DSLs, SkillBot uses:
- Pure C# and .NET idioms
- Dependency injection throughout
- Attributes for plugin discovery
- Standard Microsoft.Extensions.* patterns

**Why this matters**: .NET developers can immediately understand and extend the system without learning new paradigms.

### Technology Choices

| Choice | Reason | Alternatives Considered |
|--------|--------|------------------------|
| Semantic Kernel | Microsoft-backed, active development, auto tool calling | LangChain.NET (immature), custom LLM wrapper |
| SQLite | Single-file, zero-config, ACID compliant | In-memory only, Redis, PostgreSQL |
| Records | Immutability, value semantics, concise | Classes with readonly properties |
| xUnit | Industry standard, good VS integration | NUnit, MSTest |

---

## Architecture Philosophy

### Guiding Principles

1. **Dependency Inversion**: Core layer has no dependencies. Infrastructure depends on Core.
   - **Why**: Enables testing without mocking infrastructure
   - **Example**: `IMemoryProvider` interface in Core, `SqliteMemoryProvider` in Infrastructure

2. **Interface Segregation**: Small, focused interfaces
   - **Why**: Easier to mock, implement, and understand
   - **Example**: `IAgentEngine` vs combining it with `IPluginProvider`

3. **Single Responsibility**: Each class has one reason to change
   - **Why**: Reduces coupling, improves testability
   - **Example**: `AgentOrchestrator` only coordinates, `TaskRouter` only routes

4. **Convention over Configuration**: Attribute-based plugin discovery
   - **Why**: Less boilerplate, more intuitive
   - **Example**: `[Plugin]` attribute instead of manual registration

### Layer Responsibilities

```
┌─────────────────────────────────────┐
│ Console (Presentation)              │  ← CLI interface, minimal logic
├─────────────────────────────────────┤
│ Infrastructure (Application)        │  ← Implementations, integrations
├─────────────────────────────────────┤
│ Core (Domain)                       │  ← Interfaces, models, business rules
├─────────────────────────────────────┤
│ Plugins (Tools)                     │  ← Reusable, self-contained tools
└─────────────────────────────────────┘
```

**Critical Rule**: Dependencies flow DOWN only. Core never references Infrastructure.

---

## Key Design Decisions

### Decision 1: Records for Models

**Decision**: Use C# records instead of classes for all models.

```csharp
// ✅ Chosen approach
public record AgentMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}

// ❌ Not chosen
public class AgentMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}
```

**Why**:
- Immutability prevents accidental state mutation
- Value semantics (equality by value, not reference)
- Concise syntax with init-only properties
- Better for event sourcing / message passing

**Tradeoff**: Records are immutable, requiring `with` expressions for "updates":
```csharp
var updated = original with { Content = "new content" };
```

### Decision 2: Semantic Kernel Integration

**Decision**: Use Semantic Kernel's auto tool calling instead of manual function invocation.

```csharp
// ✅ Chosen approach: Semantic Kernel auto-invokes
var settings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};
var response = await _chatService.GetChatMessageContentAsync(
    _chatHistory, settings, _kernel);

// ❌ Not chosen: Manual function calling
if (response.RequiresFunction)
{
    var result = InvokeFunction(response.FunctionName, response.Args);
    // ... handle result
}
```

**Why**:
- Less code to maintain
- Semantic Kernel handles retry logic, error handling
- Automatic parallel function calling (when appropriate)
- Keeps up with OpenAI API changes

**Tradeoff**: Less control over invocation timing and error handling.

### Decision 3: SQLite for Persistence

**Decision**: SQLite as default persistent storage (with in-memory option).

**Why**:
- Zero configuration required
- Single file deployment
- ACID compliance
- Good enough for 99% of use cases
- Easy backup (just copy the file)

**When not to use**:
- High concurrency (>100 simultaneous users)
- Distributed systems (use PostgreSQL/Redis)
- Cloud-native (consider Cosmos DB/DynamoDB)

### Decision 4: Attribute-Based Plugin Discovery

**Decision**: Use attributes for plugin metadata instead of manual registration.

```csharp
// ✅ Chosen: Declarative
[Plugin(Name = "Calculator")]
public class CalculatorPlugin
{
    [KernelFunction("add")]
    [Description("Add two numbers")]
    public double Add(double a, double b) => a + b;
}

// ❌ Not chosen: Programmatic
pluginRegistry.Register("Calculator", "add", 
    (args) => Add(args["a"], args["b"]),
    "Add two numbers");
```

**Why**:
- Metadata co-located with code
- IDE support (IntelliSense, refactoring)
- Less boilerplate
- Reflection is acceptable here (one-time cost at startup)

### Decision 5: Multi-Agent Routing via LLM

**Decision**: Use an LLM call to route tasks to agents, not rule-based routing.

**Why**:
- Handles ambiguous requests intelligently
- Adapts to new agent types without code changes
- Can explain routing decisions
- More flexible than keyword matching

**Tradeoff**: 
- Extra LLM call adds latency (~500ms)
- Costs tokens
- Non-deterministic (same query might route differently)

**When to reconsider**: If latency < 100ms is critical, use rule-based routing as fallback.

---

## Common Patterns

### Pattern 1: Factory Pattern for DI Registration

**Where**: `ServiceCollectionExtensions.cs`

```csharp
services.AddSingleton<IAgentOrchestrator>(sp =>
{
    var router = sp.GetRequiredService<ITaskRouter>();
    var logger = sp.GetRequiredService<ILogger<AgentOrchestrator>>();
    var orchestrator = new AgentOrchestrator(router, logger);
    
    // Register agents
    orchestrator.RegisterAgent(sp.GetRequiredService<ResearchAgent>());
    orchestrator.RegisterAgent(sp.GetRequiredService<CodingAgent>());
    
    return orchestrator;
});
```

**Why this pattern**:
- Orchestrator needs agents registered after construction
- Can't use constructor injection (circular dependency risk)
- Factory ensures proper initialization order

**When to use elsewhere**: When object needs post-construction setup.

### Pattern 2: Strategy Pattern for Memory Providers

**Where**: `ServiceCollectionExtensions.RegisterMemoryProvider()`

```csharp
switch (options.MemoryProvider.ToLowerInvariant())
{
    case "sqlite":
        services.AddSingleton<IMemoryProvider, SqliteMemoryProvider>();
        break;
    case "inmemory":
    default:
        services.AddSingleton<IMemoryProvider, InMemoryProvider>();
        break;
}
```

**Why this pattern**:
- Swappable implementations
- Configuration-driven selection
- Easy to add new providers (Redis, Cosmos DB)

**How to extend**:
```csharp
case "redis":
    services.AddSingleton<IMemoryProvider, RedisMemoryProvider>();
    break;
```

### Pattern 3: Base Class for Specialized Agents

**Where**: `BaseSpecializedAgent.cs`

```csharp
public abstract class BaseSpecializedAgent : ISpecializedAgent
{
    protected readonly Kernel _kernel;
    // ... shared implementation
    
    protected abstract string GetSystemPrompt(); // Template method
}

public class ResearchAgent : BaseSpecializedAgent
{
    protected override string GetSystemPrompt() => "You are a research specialist...";
}
```

**Why this pattern**:
- Reduces duplication across agents
- Enforces consistent behavior
- Template method for customization points

**When NOT to use**: If agents need vastly different implementations (composition might be better).

### Pattern 4: ConcurrentDictionary for Thread-Safe Collections

**Where**: `DynamicPluginProvider.cs`

```csharp
private readonly ConcurrentDictionary<string, PluginMetadata> _plugins;
```

**Why**:
- Thread-safe read/write operations
- No locking overhead for reads
- Better than `Dictionary` + `lock`

**When NOT to use**: Single-threaded scenarios (use regular `Dictionary`).

### Pattern 5: Async/Await Throughout

**Critical Rule**: NEVER use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`

```csharp
// ✅ Good
public async Task<string> FetchDataAsync()
{
    return await _client.GetStringAsync(url);
}

// ❌ Bad - blocks thread
public string FetchData()
{
    return _client.GetStringAsync(url).Result; // DEADLOCK RISK
}
```

**Why**: Blocking on async causes deadlocks, wastes threads, hurts performance.

---

## File Organization

### Naming Conventions

```
Interface:        IAgentEngine.cs
Implementation:   SemanticKernelEngine.cs  (NOT AgentEngineImpl.cs)
Model:            AgentMessage.cs
Exception:        PluginException.cs
Test:             CalculatorPluginTests.cs
```

### Folder Structure Logic

```
SkillBot.Core/
├── Interfaces/          # All interfaces (I*.cs)
├── Models/              # All records/models
└── Exceptions/          # All custom exceptions

SkillBot.Infrastructure/
├── Engine/              # IAgentEngine implementations
├── Memory/              # IMemoryProvider implementations
├── Plugins/             # IPluginProvider implementations
├── MultiAgent/          # Multi-agent system
│   └── Agents/          # Specialized agent implementations
└── Configuration/       # DI and config classes

SkillBot.Plugins/
├── Examples/            # Built-in example plugins
└── [Domain]/            # Grouped by domain (OpenAI/, GitHub/, etc.)

SkillBot.Console/
└── Program.cs           # Entry point, minimal logic
```

### When to Create New Files

**Create new file when**:
- New interface definition
- New model/record type
- New implementation of existing interface
- New plugin (each plugin = separate file)

**DON'T create new file for**:
- Small helper methods (put in existing class)
- Extension methods (group related ones together)
- Test fixtures (one test file per class under test)

---

## Development Workflows

### Adding a New Plugin

1. **Create plugin class** in `SkillBot.Plugins/[Domain]/`
2. **Add attributes**: `[Plugin]`, `[KernelFunction]`, `[Description]`
3. **Register in Program.cs**: `pluginProvider.RegisterPlugin(new YourPlugin());`
4. **Write tests** in `SkillBot.Plugins.Tests/`
5. **Update docs** in `docs/PLUGIN-DEVELOPMENT.md` (if example-worthy)

**Example**:
```csharp
// 1. Create file: SkillBot.Plugins/GitHub/GitHubPlugin.cs
[Plugin(Name = "GitHub", Description = "GitHub integration")]
public class GitHubPlugin
{
    [KernelFunction("get_repo")]
    [Description("Get repository information")]
    public async Task<string> GetRepoAsync(string owner, string repo) { }
}

// 2. Register in Program.cs
pluginProvider.RegisterPlugin(new GitHubPlugin());
```

### Adding a New Specialized Agent

1. **Create agent class** in `SkillBot.Infrastructure/MultiAgent/Agents/`
2. **Inherit from** `BaseSpecializedAgent`
3. **Override abstract members**: `AgentId`, `Name`, `Description`, `Specializations`, `GetSystemPrompt()`
4. **Register in** `ServiceCollectionExtensions.AddMultiAgentOrchestration()`
5. **Test routing** with relevant queries

**Example**:
```csharp
// 1. Create: SeoAgent.cs
public class SeoAgent : BaseSpecializedAgent
{
    public override string AgentId => "seo-agent";
    public override string Name => "SEO Specialist";
    public override string Description => "SEO optimization expert";
    public override IReadOnlyList<string> Specializations => 
        new[] { "seo", "keywords", "optimization", "ranking" };
    
    protected override string GetSystemPrompt() => 
        "You are an SEO expert...";
}

// 2. Register in ServiceCollectionExtensions
services.AddSingleton<SeoAgent>();
services.AddSingleton<ISpecializedAgent>(sp => sp.GetRequiredService<SeoAgent>());

// 3. Auto-registered with orchestrator via factory
```

### Adding a New Memory Provider

1. **Create class** in `SkillBot.Infrastructure/Memory/`
2. **Implement** `IMemoryProvider`
3. **Add to switch statement** in `RegisterMemoryProvider()`
4. **Add config option** to `SkillBotOptions`
5. **Document** in deployment guide

**Example**:
```csharp
// 1. Create: RedisMemoryProvider.cs
public class RedisMemoryProvider : IMemoryProvider
{
    public async Task AddMessageAsync(AgentMessage message, ...) { }
    // ... implement interface
}

// 2. Register in ServiceCollectionExtensions
case "redis":
    services.AddSingleton<IMemoryProvider>(sp => 
        new RedisMemoryProvider(options.RedisConnectionString, logger));
    break;

// 3. Add to SkillBotOptions
public string RedisConnectionString { get; set; } = "";
```

### Modifying the Execution Flow

**⚠️ CAUTION**: The execution flow in `SemanticKernelEngine.ExecuteAsync()` is critical.

**Current flow**:
1. Add user message to `_chatHistory`
2. Save to `_memoryProvider`
3. Call LLM with auto-tool-calling enabled
4. Extract response and tool calls
5. Add assistant response to history
6. Return `AgentResponse`

**Common modifications**:
- **Add caching**: Check cache before step 3
- **Add rate limiting**: Wrap step 3 in rate limiter
- **Add retry logic**: Wrap step 3 in Polly retry policy
- **Add logging**: Add detailed logging between each step

**What NOT to change**:
- Don't remove the `_chatHistory.AddUserMessage()` call (breaks SK's context)
- Don't skip saving to memory (breaks conversation persistence)
- Don't try to manually invoke tools (SK does this automatically)

---

## Testing Strategy

### What to Test

**✅ Always test**:
- Plugin function logic (unit tests)
- Model validation/business rules
- Error handling paths
- Integration with Semantic Kernel (integration tests)

**⚠️ Sometimes test**:
- Private helper methods (if complex logic)
- Configuration parsing
- DI registration (if custom factories)

**❌ Don't test**:
- Simple properties (auto-properties)
- Framework code (Semantic Kernel, EF Core)
- Third-party libraries

### Test Organization

```
SkillBot.Tests/
├── SkillBot.Core.Tests/
│   ├── Models/
│   └── Exceptions/
├── SkillBot.Infrastructure.Tests/
│   ├── Engine/
│   ├── Memory/
│   └── MultiAgent/
└── SkillBot.Plugins.Tests/
    └── Examples/
```

### Mocking Strategy

**Use Moq for interfaces**:
```csharp
var mockMemory = new Mock<IMemoryProvider>();
mockMemory.Setup(m => m.AddMessageAsync(It.IsAny<AgentMessage>(), default))
    .ReturnsAsync(Task.CompletedTask);
```

**Use real implementations when possible**:
```csharp
// ✅ Good: Test with real in-memory provider
var memory = new InMemoryProvider(logger);

// ⚠️ Sometimes needed: Mock for complex setup
var mockMemory = new Mock<IMemoryProvider>();
```

### Integration Test Approach

```csharp
[Fact]
public async Task FullStack_PluginInvocation_Works()
{
    // Arrange: Build real DI container
    var services = new ServiceCollection();
    services.AddSkillBot(configuration);
    var provider = services.BuildServiceProvider();
    
    var engine = provider.GetRequiredService<IAgentEngine>();
    var pluginProvider = provider.GetRequiredService<IPluginProvider>();
    pluginProvider.RegisterPlugin(new CalculatorPlugin());
    
    // Act: Execute with real LLM call
    var response = await engine.ExecuteAsync("What's 5 + 3?");
    
    // Assert
    Assert.Contains("8", response.Content);
    Assert.True(response.ToolCalls.Any(tc => tc.FunctionName == "add"));
}
```

**Warning**: Integration tests hit real OpenAI API (costs money, slower). Use sparingly.

---

## Known Issues & Gotchas

### Issue 1: Semantic Kernel Version Compatibility

**Problem**: Semantic Kernel is under heavy development. Breaking changes happen.

**Current version**: 1.30.0+

**Known breaking changes**:
- Tool calling API changed in 1.0 → 1.20
- Memory connectors refactored in 1.25

**Mitigation**:
- Lock to specific version in `.csproj`
- Test thoroughly before upgrading SK
- Check SK release notes before upgrading

### Issue 2: SQLite Database Locking

**Problem**: SQLite allows one writer at a time. Multiple instances = locking errors.

**Symptoms**:
```
Microsoft.Data.Sqlite.SqliteException: database is locked
```

**Solutions**:
1. Enable WAL mode (done automatically in `SqliteMemoryProvider`)
2. Use separate database per user/session
3. Implement retry logic with exponential backoff
4. For high concurrency, use PostgreSQL instead

### Issue 3: Plugin Description Quality

**Problem**: LLM decides which plugin to call based on descriptions. Bad descriptions = wrong tool calls.

**Example**:
```csharp
// ❌ Bad: Too vague
[Description("Does stuff with files")]
public string ProcessFile(string path) { }

// ✅ Good: Specific and clear
[Description("Read the contents of a text file from disk and return as a string")]
public string ReadTextFile(string filePath) { }
```

**Best practices**:
- Be specific about what the function does
- Mention input format/constraints
- Describe return value
- Include example in description if helpful

### Issue 4: Circular Dependency in Multi-Agent System

**Problem**: Orchestrator needs agents, agents need kernel, kernel setup needs orchestrator?

**Solution**: Factory pattern registration
```csharp
// ✅ Correct: Factory breaks the cycle
services.AddSingleton<IAgentOrchestrator>(sp => {
    var orchestrator = new AgentOrchestrator(...);
    orchestrator.RegisterAgent(sp.GetRequiredService<ResearchAgent>());
    return orchestrator;
});
```

### Issue 5: Streaming with Tool Calls

**Problem**: Semantic Kernel's streaming doesn't work well with tool calls.

**Current behavior**: `ExecuteStreamingAsync()` doesn't auto-invoke tools.

**Workaround**: Use `ExecuteAsync()` for tool-heavy operations, streaming for text-only.

**Future**: This may improve in later SK versions.

### Issue 6: Token Usage Not Always Available

**Problem**: Semantic Kernel doesn't always expose token counts from OpenAI.

**Current**: `AgentResponse.TokensUsed` is often 0.

**Workaround**: 
- Use OpenAI API directly if token tracking is critical
- Estimate based on text length (rough: 1 token ≈ 4 chars)
- Check response metadata (may contain usage info)

---

## Future Considerations

### Scalability

**Current limitations**:
- Single process (not distributed)
- SQLite (not suitable for >100 concurrent users)
- No load balancing

**Future paths**:
1. **Horizontal scaling**: Add Redis for shared state, message queue for tasks
2. **Database**: Migrate to PostgreSQL for multi-user scenarios
3. **Caching**: Add response caching to reduce API costs
4. **Rate limiting**: Per-user rate limits

### Extensibility Points

**Areas designed for extension**:
- Memory providers (Redis, Cosmos DB)
- Specialized agents (domain experts)
- Plugins (unlimited tool types)
- Execution strategies (beyond single/parallel/sequential)

**NOT designed for extension** (would require refactoring):
- LLM provider (currently OpenAI-only via SK)
- Agent engine (tightly coupled to Semantic Kernel)

### API Surface

**If building REST API**:
```csharp
// Recommended approach
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentEngine _engine;
    
    [HttpPost("chat")]
    public async Task<AgentResponse> Chat([FromBody] ChatRequest request)
    {
        return await _engine.ExecuteAsync(request.Message);
    }
}
```

**Consider**:
- Authentication/authorization
- Rate limiting per API key
- Streaming responses (SSE or WebSockets)
- Conversation session management

### Performance Optimization

**Current bottlenecks**:
1. LLM API latency (1-3 seconds)
2. SQLite writes (for high concurrency)
3. Plugin execution time (if plugins are slow)

**Optimization opportunities**:
- Cache common queries/responses
- Parallel plugin execution (when safe)
- Prefetch likely next queries
- Use faster models for simple queries (gpt-3.5-turbo)

---

## Context for AI Coding Agents

### When Using This Codebase

**Remember**:
1. Core layer is pure - no infrastructure dependencies
2. All async methods must use async/await (never .Result)
3. Records are immutable - use `with` expressions
4. Plugins use attributes for metadata
5. DI registration uses factory pattern for complex setup

### Common Tasks

**"Add a new feature"**:
1. Check if it belongs in Core (interface/model) or Infrastructure (implementation)
2. Follow existing patterns (see [Common Patterns](#common-patterns))
3. Add tests
4. Update documentation

**"Fix a bug"**:
1. Write a failing test first
2. Fix the code
3. Ensure test passes
4. Check for similar bugs elsewhere

**"Refactor code"**:
1. Ensure tests exist and pass
2. Make changes
3. Tests still pass = refactor successful
4. If tests fail, revert and reconsider

### Code Generation Guidelines

**When generating new code**:
- Match existing naming conventions exactly
- Use records for models, classes for behavior
- Include XML documentation for public APIs
- Add `[Description]` attributes to plugin methods
- Use async/await consistently
- Follow the existing folder structure

**When modifying existing code**:
- Preserve the architectural layer separation
- Don't introduce dependencies from Core to Infrastructure
- Keep existing patterns consistent
- Update tests accordingly

---

## Quick Reference

### Key Files to Understand

1. **IAgentEngine.cs** - Core orchestration interface
2. **SemanticKernelEngine.cs** - Main execution loop
3. **AgentOrchestrator.cs** - Multi-agent coordination
4. **DynamicPluginProvider.cs** - Plugin discovery/registration
5. **ServiceCollectionExtensions.cs** - DI setup

### Architecture Cheat Sheet

```
User Request
    ↓
Program.cs (Console)
    ↓
IAgentEngine (Interface in Core)
    ↓
SemanticKernelEngine (Implementation in Infrastructure)
    ↓
Semantic Kernel (Framework)
    ↓
OpenAI API
    ↓
Response → User
```

### Essential Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Run
dotnet run --project SkillBot.Console

# Run with multi-agent
dotnet run --project SkillBot.Console -- --multi-agent

# Publish
dotnet publish -c Release -r linux-x64
```

---

**Document Version**: 1.0  
**Maintained By**: Development Team  
**For**: AI Coding Agents (Vibe, Cursor, Copilot, etc.)

**Remember**: This document should stay PRIVATE (gitignored). It contains internal implementation details not suitable for public documentation.
