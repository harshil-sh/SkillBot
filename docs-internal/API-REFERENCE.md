# SkillBot API Reference

## Table of Contents
- [Core Interfaces](#core-interfaces)
- [Infrastructure Classes](#infrastructure-classes)
- [Models](#models)
- [Plugins](#plugins)
- [Configuration](#configuration)

## Core Interfaces

### IAgentEngine

Main execution engine for the agent system.

```csharp
namespace SkillBot.Core.Interfaces;

public interface IAgentEngine
{
    /// <summary>
    /// Execute a single turn with user message
    /// </summary>
    Task<AgentResponse> ExecuteAsync(
        string message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute with streaming response
    /// </summary>
    IAsyncEnumerable<string> ExecuteStreamingAsync(
        string message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reset conversation history
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current execution context
    /// </summary>
    IExecutionContext Context { get; }
}
```

**Implementation**: `SkillBot.Infrastructure.Engine.SemanticKernelEngine`

**Usage Example**:
```csharp
var engine = services.GetRequiredService<IAgentEngine>();
var response = await engine.ExecuteAsync("What's 2+2?");
Console.WriteLine(response.Content); // "2 plus 2 equals 4"
```

**Thread Safety**: Not thread-safe. Create one instance per conversation.

---

### IPluginProvider

Manages plugin registration and discovery.

```csharp
namespace SkillBot.Core.Interfaces;

public interface IPluginProvider
{
    /// <summary>
    /// Register plugins from assembly
    /// </summary>
    Task RegisterPluginsFromAssemblyAsync(string assemblyPath);
    
    /// <summary>
    /// Register a single plugin
    /// </summary>
    void RegisterPlugin<TPlugin>(TPlugin instance) where TPlugin : class;
    
    /// <summary>
    /// Get all registered plugins
    /// </summary>
    IReadOnlyList<PluginMetadata> GetRegisteredPlugins();
    
    /// <summary>
    /// Get specific plugin by name
    /// </summary>
    object? GetPlugin(string pluginName);
    
    /// <summary>
    /// Unregister a plugin
    /// </summary>
    bool UnregisterPlugin(string pluginName);
}
```

**Implementation**: `SkillBot.Infrastructure.Plugins.DynamicPluginProvider`

**Usage Example**:
```csharp
var provider = services.GetRequiredService<IPluginProvider>();

// Register single plugin
provider.RegisterPlugin(new CalculatorPlugin());

// Register from assembly
await provider.RegisterPluginsFromAssemblyAsync("./MyPlugins.dll");

// List plugins
var plugins = provider.GetRegisteredPlugins();
foreach (var plugin in plugins)
{
    Console.WriteLine($"{plugin.Name}: {plugin.Description}");
}
```

**Thread Safety**: Thread-safe for reads, synchronized writes.

---

### IMemoryProvider

Abstracts conversation history storage.

```csharp
namespace SkillBot.Core.Interfaces;

public interface IMemoryProvider
{
    /// <summary>
    /// Add message to history
    /// </summary>
    Task AddMessageAsync(
        AgentMessage message, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieve conversation history
    /// </summary>
    /// <param name="count">Max messages to return (null = all)</param>
    Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        int? count = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all history
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get total message count
    /// </summary>
    Task<int> GetMessageCountAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save to persistent storage
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
```

**Implementations**:
- `SkillBot.Infrastructure.Memory.InMemoryProvider`
- `SkillBot.Infrastructure.Memory.SqliteMemoryProvider`

**Usage Example**:
```csharp
var memory = services.GetRequiredService<IMemoryProvider>();

// Add messages
await memory.AddMessageAsync(new AgentMessage 
{ 
    Role = "user", 
    Content = "Hello" 
});

// Retrieve last 10 messages
var recent = await memory.GetHistoryAsync(count: 10);

// Clear history
await memory.ClearAsync();
```

**Thread Safety**: Implementation-dependent. SqliteMemoryProvider is thread-safe.

---

### IAgentOrchestrator

Coordinates multiple specialized agents.

```csharp
namespace SkillBot.Core.Interfaces;

public interface IAgentOrchestrator
{
    /// <summary>
    /// Register a specialized agent
    /// </summary>
    void RegisterAgent(ISpecializedAgent agent);
    
    /// <summary>
    /// Unregister an agent
    /// </summary>
    bool UnregisterAgent(string agentId);
    
    /// <summary>
    /// Get all registered agents
    /// </summary>
    IReadOnlyList<ISpecializedAgent> GetAgents();
    
    /// <summary>
    /// Execute task with agent coordination
    /// </summary>
    Task<OrchestratedResponse> ExecuteTaskAsync(
        string userRequest,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute multi-step workflow
    /// </summary>
    Task<OrchestratedResponse> ExecuteWorkflowAsync(
        AgentWorkflow workflow,
        CancellationToken cancellationToken = default);
}
```

**Implementation**: `SkillBot.Infrastructure.MultiAgent.AgentOrchestrator`

**Usage Example**:
```csharp
var orchestrator = services.GetRequiredService<IAgentOrchestrator>();

// Execute task (routing is automatic)
var response = await orchestrator.ExecuteTaskAsync(
    "Research Python and write a summary"
);

Console.WriteLine(response.FinalResponse);
Console.WriteLine($"Used {response.AgentResults.Count} agents");
```

---

### ISpecializedAgent

Contract for domain-specific agents.

```csharp
namespace SkillBot.Core.Interfaces;

public interface ISpecializedAgent
{
    string AgentId { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<string> Specializations { get; }
    
    /// <summary>
    /// Check if agent can handle task
    /// </summary>
    Task<bool> CanHandleAsync(
        AgentTask task, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute assigned task
    /// </summary>
    Task<AgentTaskResult> ExecuteAsync(
        AgentTask task, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current agent status
    /// </summary>
    AgentStatus GetStatus();
}
```

**Base Implementation**: `SkillBot.Infrastructure.MultiAgent.Agents.BaseSpecializedAgent`

**Concrete Implementations**:
- `ResearchAgent`
- `CodingAgent`
- `DataAnalysisAgent`
- `WritingAgent`

**Usage Example**:
```csharp
public class MyAgent : BaseSpecializedAgent
{
    public override string AgentId => "my-agent";
    public override string Name => "My Specialist";
    public override string Description => "Expert in X";
    public override IReadOnlyList<string> Specializations => 
        new[] { "keyword1", "keyword2" };
    
    protected override string GetSystemPrompt()
    {
        return "You are an expert in...";
    }
}
```

---

### ITaskRouter

Routes tasks to appropriate agents.

```csharp
namespace SkillBot.Core.Interfaces;

public interface ITaskRouter
{
    /// <summary>
    /// Analyze request and determine routing
    /// </summary>
    Task<TaskRoutingDecision> RouteTaskAsync(
        string userRequest,
        IReadOnlyList<ISpecializedAgent> availableAgents,
        CancellationToken cancellationToken = default);
}
```

**Implementation**: `SkillBot.Infrastructure.MultiAgent.LlmTaskRouter`

**Routing Strategies**:
- `single`: One agent handles entire task
- `parallel`: Multiple agents work simultaneously
- `sequential`: Agents work in order, building on previous results

---

## Infrastructure Classes

### SemanticKernelEngine

Main implementation of `IAgentEngine`.

```csharp
namespace SkillBot.Infrastructure.Engine;

public class SemanticKernelEngine : IAgentEngine
{
    public SemanticKernelEngine(
        Kernel kernel,
        IChatCompletionService chatService,
        IMemoryProvider memoryProvider,
        IPluginProvider pluginProvider,
        ILogger<SemanticKernelEngine> logger)
    {
        // Constructor
    }
    
    // Implements IAgentEngine methods
}
```

**Configuration**:
```json
{
  "SkillBot": {
    "ApiKey": "sk-...",
    "Model": "gpt-4"
  }
}
```

**Features**:
- Automatic tool calling via Semantic Kernel
- Conversation history management
- Streaming support
- Execution context tracking

---

### SqliteMemoryProvider

Persistent memory implementation using SQLite.

```csharp
namespace SkillBot.Infrastructure.Memory;

public class SqliteMemoryProvider : IMemoryProvider, IAsyncDisposable
{
    public SqliteMemoryProvider(
        string databasePath,
        ILogger<SqliteMemoryProvider> logger)
    {
        // Constructor
    }
    
    // Additional methods
    public async Task VacuumAsync(CancellationToken cancellationToken = default);
    public async Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default);
}
```

**Database Schema**:
```sql
CREATE TABLE messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    role TEXT NOT NULL,
    content TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    metadata TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_messages_timestamp ON messages(timestamp DESC);
CREATE INDEX idx_messages_role ON messages(role);
```

**Configuration**:
```json
{
  "SkillBot": {
    "MemoryProvider": "SQLite",
    "SqliteDatabasePath": "skillbot.db"
  }
}
```

---

### AgentOrchestrator

Coordinates multiple agents.

```csharp
namespace SkillBot.Infrastructure.MultiAgent;

public class AgentOrchestrator : IAgentOrchestrator
{
    public AgentOrchestrator(
        ITaskRouter taskRouter,
        ILogger<AgentOrchestrator> logger)
    {
        // Constructor
    }
    
    // Private execution strategies
    private Task<List<AgentTaskResult>> ExecuteSingleAgentAsync(...);
    private Task<List<AgentTaskResult>> ExecuteParallelAsync(...);
    private Task<List<AgentTaskResult>> ExecuteSequentialAsync(...);
}
```

**Execution Flow**:
1. User request → Router
2. Router returns `TaskRoutingDecision`
3. Orchestrator executes based on strategy
4. Results synthesized into final response

---

## Models

### AgentMessage

```csharp
namespace SkillBot.Core.Models;

public record AgentMessage
{
    public required string Role { get; init; }        // "user", "assistant", "system"
    public required string Content { get; init; }     // Message text
    public DateTimeOffset Timestamp { get; init; }    // When created
    public Dictionary<string, object>? Metadata { get; init; } // Optional metadata
}
```

---

### AgentResponse

```csharp
namespace SkillBot.Core.Models;

public record AgentResponse
{
    public required string Content { get; init; }         // LLM response
    public List<ToolCall> ToolCalls { get; init; }       // Tools used
    public TimeSpan ExecutionTime { get; init; }          // How long it took
    public int TokensUsed { get; init; }                  // Tokens consumed
    public bool IsSuccess { get; init; } = true;          // Success flag
    public string? ErrorMessage { get; init; }            // Error if failed
}
```

---

### ToolCall

```csharp
namespace SkillBot.Core.Models;

public record ToolCall
{
    public required string PluginName { get; init; }      // Plugin that was called
    public required string FunctionName { get; init; }    // Function name
    public Dictionary<string, object>? Arguments { get; init; } // Function args
    public object? Result { get; init; }                  // Function result
    public TimeSpan ExecutionTime { get; init; }          // Execution time
}
```

---

### OrchestratedResponse

```csharp
namespace SkillBot.Core.Models;

public record OrchestratedResponse
{
    public required string FinalResponse { get; init; }   // Synthesized response
    public List<AgentTaskResult> AgentResults { get; init; } // Individual results
    public TimeSpan TotalExecutionTime { get; init; }     // Total time
    public bool IsSuccess { get; init; } = true;          // Success flag
    public string? ErrorMessage { get; init; }            // Error if failed
    public Dictionary<string, object>? Metadata { get; init; } // Additional info
}
```

---

## Plugins

### Plugin Attributes

```csharp
// Mark class as plugin
[Plugin(Name = "MyPlugin", Description = "What it does")]
public class MyPlugin { }

// Mark method as callable function
[KernelFunction("function_name")]
[Description("What this function does")]
public string MyFunction() { }

// Describe parameters
public string MyFunction(
    [Description("What this param is")] string input
) { }
```

### Example Plugin

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SkillBot.Plugins.Examples;

[Plugin(Name = "Calculator", Description = "Basic math operations")]
public class CalculatorPlugin
{
    [KernelFunction("add")]
    [Description("Add two numbers together")]
    public double Add(
        [Description("First number")] double a,
        [Description("Second number")] double b)
    {
        return a + b;
    }
    
    [KernelFunction("multiply")]
    [Description("Multiply two numbers")]
    public double Multiply(
        [Description("First number")] double a,
        [Description("Second number")] double b)
    {
        return a * b;
    }
}
```

### Async Plugin Methods

```csharp
[KernelFunction("fetch_data")]
[Description("Fetch data from API")]
public async Task<string> FetchDataAsync(
    [Description("API endpoint")] string url)
{
    using var client = new HttpClient();
    return await client.GetStringAsync(url);
}
```

### Plugin with Dependencies

```csharp
public class DatabasePlugin
{
    private readonly IConfiguration _config;
    private readonly ILogger<DatabasePlugin> _logger;
    
    public DatabasePlugin(
        IConfiguration config,
        ILogger<DatabasePlugin> logger)
    {
        _config = config;
        _logger = logger;
    }
    
    [KernelFunction("query")]
    [Description("Query database")]
    public async Task<string> QueryAsync(
        [Description("SQL query")] string query)
    {
        // Implementation
    }
}

// Register with DI
services.AddSingleton<DatabasePlugin>();
pluginProvider.RegisterPlugin(services.GetRequiredService<DatabasePlugin>());
```

---

## Configuration

### SkillBotOptions

```csharp
namespace SkillBot.Infrastructure.Configuration;

public class SkillBotOptions
{
    public const string SectionName = "SkillBot";
    
    // OpenAI Configuration
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    
    // Azure OpenAI (optional)
    public string? AzureEndpoint { get; set; }
    public string? AzureDeploymentName { get; set; }
    
    // Memory Configuration
    public string MemoryProvider { get; set; } = "InMemory"; // or "SQLite"
    public string SqliteDatabasePath { get; set; } = "skillbot.db";
    
    // General Settings
    public int MaxHistoryMessages { get; set; } = 100;
    public bool VerboseLogging { get; set; } = false;
    public List<string> PluginAssemblyPaths { get; set; } = new();
}
```

### Service Registration

```csharp
// In Program.cs
services.AddSkillBot(configuration);
services.AddMultiAgentOrchestration(); // Optional

// In ServiceCollectionExtensions.cs
public static IServiceCollection AddSkillBot(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Bind options
    var options = new SkillBotOptions();
    configuration.GetSection(SkillBotOptions.SectionName).Bind(options);
    services.AddSingleton(options);
    
    // Register services...
    return services;
}
```

---

## Error Handling

### Exception Hierarchy

```csharp
AgentException              // Base exception
├── PluginException        // Plugin-related errors
├── MemoryException        // Memory storage errors
└── ExecutionException     // Engine execution errors
```

### Example Error Handling

```csharp
try
{
    var response = await engine.ExecuteAsync("user message");
}
catch (PluginException ex)
{
    Console.WriteLine($"Plugin error: {ex.Message}");
    Console.WriteLine($"Plugin: {ex.PluginName}");
}
catch (MemoryException ex)
{
    Console.WriteLine($"Memory error: {ex.Message}");
}
catch (AgentException ex)
{
    Console.WriteLine($"Agent error: {ex.Message}");
}
```

---

## Best Practices

### 1. Resource Disposal

```csharp
// SqliteMemoryProvider implements IAsyncDisposable
await using var memoryProvider = new SqliteMemoryProvider(...);
```

### 2. Cancellation Tokens

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await engine.ExecuteAsync("message", cts.Token);
```

### 3. Plugin Parameter Validation

```csharp
[KernelFunction("divide")]
public double Divide(double a, double b)
{
    if (b == 0)
        throw new ArgumentException("Division by zero", nameof(b));
    return a / b;
}
```

### 4. Async Best Practices

```csharp
// ✅ Good: Use ConfigureAwait(false) in libraries
await SomeMethodAsync().ConfigureAwait(false);

// ✅ Good: Async all the way
public async Task<string> MyMethodAsync()
{
    return await SomeAsyncOperation();
}

// ❌ Bad: Blocking on async
var result = MyMethodAsync().Result; // Don't do this!
```

---

**API Version**: 1.0  
**Last Updated**: 2026-04-16  
**Compatibility**: .NET 10.0+
