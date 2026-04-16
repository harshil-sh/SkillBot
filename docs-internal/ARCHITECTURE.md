# SkillBot Architecture Documentation

## Table of Contents
- [Overview](#overview)
- [Architecture Layers](#architecture-layers)
- [Design Patterns](#design-patterns)
- [Component Diagrams](#component-diagrams)
- [Data Flow](#data-flow)
- [Extension Points](#extension-points)

## Overview

SkillBot follows a **Clean Architecture** pattern with clear separation of concerns across four distinct layers.

### Architecture Principles

1. **Dependency Inversion**: Dependencies flow inward (Infrastructure → Core)
2. **Interface Segregation**: Small, focused interfaces
3. **Single Responsibility**: Each class has one reason to change
4. **Open/Closed**: Open for extension, closed for modification
5. **Liskov Substitution**: Implementations are interchangeable

### Key Design Goals

- **Testability**: Pure domain logic, easy to mock dependencies
- **Extensibility**: Add plugins/agents without modifying core
- **Maintainability**: Clear boundaries, minimal coupling
- **Flexibility**: Swap implementations (memory, LLM providers)

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                   (SkillBot.Console)                     │
│  • CLI interface                                         │
│  • User input/output                                     │
│  • Command parsing                                       │
└────────────────┬────────────────────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────────────────────┐
│                   Application Layer                      │
│                 (SkillBot.Infrastructure)                │
│  • Engine implementations                                │
│  • Memory providers                                      │
│  • Plugin system                                         │
│  • Multi-agent orchestration                            │
└────────────────┬────────────────────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────────────────────┐
│                     Domain Layer                         │
│                    (SkillBot.Core)                       │
│  • Pure interfaces                                       │
│  • Domain models                                         │
│  • Business rules                                        │
│  • NO external dependencies                              │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                     Plugin Layer                         │
│                   (SkillBot.Plugins)                     │
│  • Tool implementations                                  │
│  • Reusable skills                                       │
│  • Third-party integrations                              │
└──────────────────────────────────────────────────────────┘
```

### Layer 1: Core (Domain)

**Location**: `SkillBot.Core/`  
**Dependencies**: None  
**Purpose**: Define contracts and domain models

#### Interfaces

```csharp
// Core orchestration
IAgentEngine           // Main execution engine
IPluginProvider        // Plugin management
IMemoryProvider        // Conversation storage
IExecutionContext      // Runtime metadata

// Multi-agent system
IAgentOrchestrator     // Coordinates agents
ISpecializedAgent      // Individual agent contract
ITaskRouter            // Routes tasks to agents
```

#### Models

All models use C# records for immutability:

```csharp
public record AgentMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
```

**Key Models**:
- `AgentMessage`: Single message in conversation
- `AgentResponse`: Response from engine
- `ToolCall`: Details of tool invocation
- `PluginMetadata`: Plugin information
- `AgentTask`: Task for specialized agent
- `OrchestratedResponse`: Multi-agent result

#### Exceptions

Custom exception hierarchy:
```
AgentException (base)
├── PluginException
├── MemoryException
└── ExecutionException
```

### Layer 2: Infrastructure (Application)

**Location**: `SkillBot.Infrastructure/`  
**Dependencies**: Core, Semantic Kernel, Microsoft.Extensions.*  
**Purpose**: Implement core interfaces

#### Engine

**SemanticKernelEngine** implements `IAgentEngine`:
- Manages `ChatHistory` with Semantic Kernel
- Auto-invokes tools via `ToolCallBehavior`
- Handles streaming responses
- Tracks execution context

```csharp
public class SemanticKernelEngine : IAgentEngine
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;
    
    public async Task<AgentResponse> ExecuteAsync(string message, ...)
    {
        _chatHistory.AddUserMessage(message);
        var result = await _chatService.GetChatMessageContentAsync(...);
        return new AgentResponse { ... };
    }
}
```

#### Memory Providers

Two implementations:

**InMemoryProvider**:
- Uses `ConcurrentBag<AgentMessage>`
- Fast, no I/O
- Lost on restart

**SqliteMemoryProvider**:
- Persists to SQLite database
- Survives restarts
- Uses WAL mode for concurrency

```sql
CREATE TABLE messages (
    id INTEGER PRIMARY KEY,
    role TEXT NOT NULL,
    content TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    metadata TEXT
);
```

#### Plugin System

**DynamicPluginProvider** implements `IPluginProvider`:
- Uses reflection to discover plugins
- Registers with Semantic Kernel
- Extracts metadata from attributes

```csharp
[Plugin(Name = "Calculator")]
public class CalculatorPlugin
{
    [KernelFunction("add")]
    [Description("Add two numbers")]
    public double Add(double a, double b) => a + b;
}
```

#### Multi-Agent System

**AgentOrchestrator** coordinates agents:
- Receives user request
- Consults `ITaskRouter` for routing decision
- Executes strategy (single/parallel/sequential)
- Synthesizes final response

**LlmTaskRouter** uses LLM to decide routing:
- Analyzes user intent
- Selects appropriate agent(s)
- Determines execution strategy
- Returns `TaskRoutingDecision`

**Specialized Agents** inherit from `BaseSpecializedAgent`:
- ResearchAgent: Information gathering
- CodingAgent: Programming tasks
- DataAnalysisAgent: Numerical analysis
- WritingAgent: Content creation

### Layer 3: Plugins

**Location**: `SkillBot.Plugins/`  
**Dependencies**: Core, Semantic Kernel  
**Purpose**: Reusable tool implementations

Structure:
```
SkillBot.Plugins/
├── Examples/
│   ├── CalculatorPlugin.cs
│   ├── WeatherPlugin.cs
│   └── TimePlugin.cs
└── OpenAI/
    └── SimpleUsagePlugin.cs
```

Each plugin is self-contained and independently testable.

### Layer 4: Console (Presentation)

**Location**: `SkillBot.Console/`  
**Dependencies**: All layers  
**Purpose**: User interface and application host

**Program.cs** responsibilities:
- Configure DI container
- Register services
- Handle command-line arguments
- Run agent loop
- Display results

## Design Patterns

### 1. Strategy Pattern

**Where**: Memory providers, agent routing strategies

```csharp
// Strategy interface
public interface IMemoryProvider { ... }

// Concrete strategies
public class InMemoryProvider : IMemoryProvider { ... }
public class SqliteMemoryProvider : IMemoryProvider { ... }

// Context selects strategy
services.AddSingleton<IMemoryProvider>(
    options.MemoryProvider == "SQLite" 
        ? new SqliteMemoryProvider(...) 
        : new InMemoryProvider(...)
);
```

### 2. Factory Pattern

**Where**: Agent creation, plugin instantiation

```csharp
services.AddSingleton<IAgentOrchestrator>(sp =>
{
    var orchestrator = new AgentOrchestrator(...);
    orchestrator.RegisterAgent(sp.GetRequiredService<ResearchAgent>());
    orchestrator.RegisterAgent(sp.GetRequiredService<CodingAgent>());
    return orchestrator;
});
```

### 3. Repository Pattern

**Where**: Memory providers abstract data access

```csharp
public interface IMemoryProvider
{
    Task AddMessageAsync(AgentMessage message);
    Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(int? count);
    Task ClearAsync();
}
```

### 4. Observer Pattern

**Where**: Execution context tracks events

```csharp
public interface IExecutionContext
{
    int TurnCount { get; }
    int ToolCallCount { get; }
    // Could add: event EventHandler<ToolCallEvent> OnToolCall;
}
```

### 5. Decorator Pattern

**Where**: Logging, caching can wrap engines

```csharp
public class LoggingAgentEngine : IAgentEngine
{
    private readonly IAgentEngine _inner;
    private readonly ILogger _logger;
    
    public async Task<AgentResponse> ExecuteAsync(...)
    {
        _logger.LogInformation("Executing...");
        var result = await _inner.ExecuteAsync(...);
        _logger.LogInformation("Completed");
        return result;
    }
}
```

### 6. Chain of Responsibility

**Where**: Sequential agent execution

```csharp
foreach (var agentId in routingDecision.SelectedAgentIds)
{
    var result = await agent.ExecuteAsync(task);
    context[$"{agentId}_result"] = result.Result; // Pass to next
}
```

## Component Diagrams

### Single-Agent Flow

```
User Input
    │
    ▼
┌───────────────┐
│  AgentEngine  │
└───────┬───────┘
        │
        ├─────────────────┐
        │                 │
        ▼                 ▼
┌──────────────┐  ┌──────────────┐
│ ChatHistory  │  │ PluginSystem │
│ (SK)         │  │              │
└──────┬───────┘  └──────┬───────┘
       │                 │
       ▼                 ▼
┌──────────────┐  ┌──────────────┐
│ LLM (OpenAI) │  │   Plugins    │
└──────┬───────┘  └──────┬───────┘
       │                 │
       └────────┬────────┘
                ▼
          AgentResponse
```

### Multi-Agent Flow

```
User Request
    │
    ▼
┌────────────────┐
│  Orchestrator  │
└────────┬───────┘
         │
         ▼
┌────────────────┐
│  TaskRouter    │ ← Uses LLM to decide
└────────┬───────┘
         │
         ▼
  Routing Decision
   (Strategy + Agents)
         │
    ┌────┴────┬─────────┬────────┐
    ▼         ▼         ▼        ▼
┌─────────┐ ┌──────┐ ┌──────┐ ┌────────┐
│Research │ │Coding│ │ Data │ │Writing │
│ Agent   │ │Agent │ │Agent │ │ Agent  │
└────┬────┘ └──┬───┘ └──┬───┘ └───┬────┘
     │         │        │         │
     └─────────┴────────┴─────────┘
                  │
                  ▼
         Synthesized Response
```

### Dependency Injection

```
IServiceCollection
        │
        ├─ Kernel (Singleton)
        │   └─ Configured with OpenAI
        │
        ├─ IAgentEngine → SemanticKernelEngine
        │   └─ Depends on: Kernel, IChatCompletionService
        │
        ├─ IMemoryProvider → SqliteMemoryProvider
        │   └─ Depends on: ILogger
        │
        ├─ IPluginProvider → DynamicPluginProvider
        │   └─ Depends on: Kernel, ILogger
        │
        └─ IAgentOrchestrator → AgentOrchestrator
            └─ Depends on: ITaskRouter, ILogger
            └─ Registers: All ISpecializedAgent instances
```

## Data Flow

### Message Processing Flow

```
1. User Input
   └─> "What's 25 * 17?"

2. Engine adds to ChatHistory
   └─> ChatHistory: [System, User("What's 25 * 17?")]

3. Engine calls LLM with auto-tool-calling
   └─> LLM decides to use Calculator.Multiply

4. Semantic Kernel auto-invokes plugin
   └─> Calculator.Multiply(25, 17) → 425

5. LLM generates response with result
   └─> "25 multiplied by 17 equals 425"

6. Engine returns AgentResponse
   └─> Content: "25 multiplied by 17 equals 425"
       ToolCalls: [Calculator.Multiply]
       ExecutionTime: 1.2s

7. Response displayed to user
```

### Multi-Agent Routing Flow

```
1. User: "Research Python vs JavaScript and write comparison"

2. Orchestrator → TaskRouter (LLM call)
   LLM analyzes: "Needs research + writing"
   Returns: {
     strategy: "sequential",
     agents: ["research-agent", "writing-agent"],
     reasoning: "Research first, then writing"
   }

3. Execute Sequential Strategy:
   
   Step 1: Research Agent
   Input: "Research Python vs JavaScript"
   Output: "Python: ..., JavaScript: ..."
   Context["research_result"] = output
   
   Step 2: Writing Agent
   Input: "Write comparison"
   Context: Includes research_result
   Output: "Comparison article: ..."

4. Orchestrator synthesizes final response
   Combines both agent outputs
   
5. Return to user with metadata
```

## Extension Points

### Adding New Memory Provider

```csharp
// 1. Implement interface
public class RedisMemoryProvider : IMemoryProvider
{
    // Implementation
}

// 2. Register in ServiceCollectionExtensions
case "redis":
    services.AddSingleton<IMemoryProvider, RedisMemoryProvider>();
    break;

// 3. Configure in appsettings.json
"MemoryProvider": "redis"
```

### Adding New Specialized Agent

```csharp
// 1. Create agent class
public class SeoAgent : BaseSpecializedAgent
{
    public override string AgentId => "seo-agent";
    // ... implement abstract members
}

// 2. Register in AddMultiAgentOrchestration
services.AddSingleton<SeoAgent>();
services.AddSingleton<ISpecializedAgent>(sp => 
    sp.GetRequiredService<SeoAgent>());

// 3. Auto-registered with orchestrator
```

### Adding New Plugin

```csharp
// 1. Create plugin class with attributes
[Plugin(Name = "GitHub", Description = "...")]
public class GitHubPlugin
{
    [KernelFunction("create_issue")]
    [Description("Create GitHub issue")]
    public async Task<string> CreateIssue(...)
    {
        // Implementation
    }
}

// 2. Register in Program.cs
pluginProvider.RegisterPlugin(new GitHubPlugin());
```

### Swapping LLM Provider

```csharp
// In ServiceCollectionExtensions, AddSkillBot method:

// Current: OpenAI
builder.AddOpenAIChatCompletion(modelId: options.Model, apiKey: options.ApiKey);

// Switch to: Azure OpenAI
builder.AddAzureOpenAIChatCompletion(
    deploymentName: options.AzureDeploymentName,
    endpoint: options.AzureEndpoint,
    apiKey: options.ApiKey);

// Or: Anthropic Claude
builder.AddAnthropicChatCompletion(modelId: "claude-3", apiKey: options.ApiKey);
```

## Performance Characteristics

### Time Complexity

- Plugin invocation: O(1) dictionary lookup
- Memory retrieval: O(n) where n = message count (with SQLite indexes)
- Agent routing: O(1) LLM call + O(m) where m = agent count

### Space Complexity

- In-memory provider: O(n) messages in RAM
- SQLite provider: O(n) messages on disk
- Agent count: O(m) agents in memory

### Scalability Considerations

- **Horizontal**: Each user session is independent
- **Vertical**: Limited by LLM API rate limits
- **Concurrent**: SQLite WAL mode supports multiple readers

## Security Considerations

### Current Implementation

✅ API keys in user secrets (dev) / environment variables (prod)  
✅ Input validation via parameter attributes  
✅ Exception handling prevents info leakage  
⚠️ No rate limiting  
⚠️ No user authentication  
⚠️ No input sanitization for SQL (uses parameterized queries)  

### Future Enhancements

- Add rate limiting per user/session
- Implement authentication/authorization
- Add request validation middleware
- Encrypt sensitive data at rest
- Add audit logging

---

**Document Version**: 1.0  
**Last Updated**: 2026-04-16  
**Next Review**: When adding new architectural components
