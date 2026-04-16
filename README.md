# SkillBot - AI Agentic Runtime

## Project Overview

**SkillBot** is a production-ready AI agent framework built with C# and .NET 10. It provides a clean, extensible architecture for building intelligent agents that can use tools, maintain conversation history, and coordinate multiple specialized agents to solve complex tasks.

### Key Capabilities

- ✅ **Single-Agent Mode**: Traditional AI assistant with plugin-based tool usage
- ✅ **Multi-Agent Orchestration**: Multiple specialized AI agents working together
- ✅ **Plugin System**: Dynamic tool registration with reflection-based discovery
- ✅ **Persistent Memory**: SQLite-based conversation history (optional in-memory mode)
- ✅ **Semantic Kernel Integration**: Leverages Microsoft's SK for LLM orchestration
- ✅ **Dependency Injection**: Full DI support throughout the application
- ✅ **Modern C#**: Records, nullable reference types, async/await patterns

### Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10.0 |
| Language | C# | 12+ |
| LLM Framework | Microsoft Semantic Kernel | Latest |
| Database | SQLite (optional) | - |
| DI Container | Microsoft.Extensions.DependencyInjection | 8.0+ |
| Configuration | Microsoft.Extensions.Configuration | 8.0+ |
| Logging | Microsoft.Extensions.Logging | 8.0+ |

### Architecture Style

- **Layered Architecture** (Core → Infrastructure → Plugins → Console)
- **Clean Architecture** principles
- **SOLID** design patterns
- **Dependency Inversion** throughout

### Project Status

- ✅ Core agent engine: **Complete**
- ✅ Plugin system: **Complete**
- ✅ Multi-agent orchestration: **Complete**
- ✅ SQLite persistence: **Complete**
- ⚠️ REST API: **Not implemented**
- ⚠️ Web UI: **Not implemented**
- ⚠️ RAG/Vector DB: **Not implemented**

### Quick Links

- [Architecture Guide](./ARCHITECTURE.md)
- [API Reference](./API-REFERENCE.md)
- [Plugin Development Guide](./PLUGIN-DEVELOPMENT.md)
- [Multi-Agent Guide](./MULTI-AGENT-GUIDE.md)
- [Deployment Guide](./DEPLOYMENT.md)

## Getting Started

### Prerequisites

```bash
# Required
.NET 10 SDK

# Optional
SQLite Browser (for database inspection)
Visual Studio 2022 / VS Code / Rider
```

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd SkillBot

# Restore packages
dotnet restore

# Configure API key
cd SkillBot.Console
dotnet user-secrets set "SkillBot:ApiKey" "your-openai-api-key"

# Build
dotnet build

# Run (single-agent mode)
dotnet run

# Run (multi-agent mode)
dotnet run -- --multi-agent
```

### Basic Usage

```bash
# Single agent mode
dotnet run

You: What's 25 * 17?
Assistant: 25 multiplied by 17 equals 425.

# Multi-agent mode
dotnet run -- --multi-agent

You: Research Python vs JavaScript and write a comparison
🤖 Coordinating specialists...
[Multiple agents collaborate to answer]
```

## Project Structure

```
SkillBot/
├── SkillBot.Core/                 # Pure domain layer (no dependencies)
│   ├── Interfaces/                # Core contracts
│   │   ├── IAgentEngine.cs
│   │   ├── IPluginProvider.cs
│   │   ├── IMemoryProvider.cs
│   │   ├── IExecutionContext.cs
│   │   ├── IAgentOrchestrator.cs
│   │   ├── ISpecializedAgent.cs
│   │   └── ITaskRouter.cs
│   ├── Models/                    # Domain models (records)
│   │   ├── AgentMessage.cs
│   │   ├── AgentResponse.cs
│   │   ├── ToolCall.cs
│   │   ├── PluginMetadata.cs
│   │   ├── AgentTask.cs
│   │   └── OrchestratedResponse.cs
│   └── Exceptions/                # Custom exceptions
│       ├── AgentException.cs
│       ├── PluginException.cs
│       └── MemoryException.cs
│
├── SkillBot.Infrastructure/       # Implementation layer
│   ├── Engine/
│   │   └── SemanticKernelEngine.cs
│   ├── Memory/
│   │   ├── InMemoryProvider.cs
│   │   └── SqliteMemoryProvider.cs
│   ├── Plugins/
│   │   └── DynamicPluginProvider.cs
│   ├── MultiAgent/
│   │   ├── AgentOrchestrator.cs
│   │   ├── LlmTaskRouter.cs
│   │   └── Agents/
│   │       ├── BaseSpecializedAgent.cs
│   │       ├── ResearchAgent.cs
│   │       ├── CodingAgent.cs
│   │       ├── DataAnalysisAgent.cs
│   │       └── WritingAgent.cs
│   └── Configuration/
│       ├── SkillBotOptions.cs
│       └── ServiceCollectionExtensions.cs
│
├── SkillBot.Plugins/              # Plugin implementations
│   ├── Examples/
│   │   ├── CalculatorPlugin.cs
│   │   ├── WeatherPlugin.cs
│   │   └── TimePlugin.cs
│   └── OpenAI/
│       └── SimpleUsagePlugin.cs
│
├── SkillBot.Console/              # Console host application
│   ├── Program.cs
│   └── appsettings.json
│
└── docs/                          # Documentation
    ├── README.md
    ├── ARCHITECTURE.md
    ├── API-REFERENCE.md
    ├── PLUGIN-DEVELOPMENT.md
    └── MULTI-AGENT-GUIDE.md
```

## Configuration

### appsettings.json

```json
{
  "SkillBot": {
    "ApiKey": "your-openai-api-key-here",
    "Model": "gpt-4",
    "AzureEndpoint": null,
    "AzureDeploymentName": null,
    "MaxHistoryMessages": 100,
    "VerboseLogging": false,
    "PluginAssemblyPaths": [],
    "MemoryProvider": "SQLite",
    "SqliteDatabasePath": "skillbot.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment Variables

```bash
# OpenAI Configuration
export SkillBot__ApiKey="sk-..."
export SkillBot__Model="gpt-4"

# Memory Configuration
export SkillBot__MemoryProvider="SQLite"
export SkillBot__SqliteDatabasePath="/data/skillbot.db"
```

## Core Concepts

### 1. Agent Engine

The `IAgentEngine` is the core orchestration layer that:
- Manages conversation loop with LLM
- Handles tool/plugin invocation
- Maintains execution context
- Supports streaming responses

### 2. Plugins (Tools)

Plugins are C# classes decorated with attributes:
- `[Plugin]` attribute marks the class
- `[KernelFunction]` marks callable methods
- `[Description]` provides metadata for LLM

### 3. Memory Providers

Abstract conversation storage:
- **InMemoryProvider**: Fast, ephemeral
- **SqliteMemoryProvider**: Persistent, survives restarts

### 4. Multi-Agent System

Coordinate multiple specialized agents:
- **Orchestrator**: Manages agent coordination
- **Task Router**: LLM-based routing decisions
- **Specialized Agents**: Domain experts (Research, Coding, Data, Writing)

## Development Workflow

### Adding a New Plugin

```csharp
[Plugin(Name = "MyPlugin", Description = "Does something useful")]
public class MyPlugin
{
    [KernelFunction("my_function")]
    [Description("Description for the LLM")]
    public string MyFunction([Description("Parameter description")] string input)
    {
        return $"Processed: {input}";
    }
}

// Register in Program.cs
pluginProvider.RegisterPlugin(new MyPlugin());
```

### Adding a New Specialized Agent

```csharp
public class MySpecializedAgent : BaseSpecializedAgent
{
    public override string AgentId => "my-agent";
    public override string Name => "My Specialist";
    public override string Description => "Expert in X";
    public override IReadOnlyList<string> Specializations => new[] { "keyword1", "keyword2" };

    protected override string GetSystemPrompt()
    {
        return "You are a specialist in...";
    }
}

// Register in ServiceCollectionExtensions
services.AddSingleton<MySpecializedAgent>();
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~CalculatorTests"
```

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
COPY publish/ /app
WORKDIR /app
ENTRYPOINT ["dotnet", "SkillBot.Console.dll"]
```

### Standalone

```bash
# Publish self-contained
dotnet publish -c Release -r win-x64 --self-contained

# Run
./SkillBot.Console.exe
```

## Performance Considerations

- **Token Usage**: Each agent call consumes API tokens
- **Parallel Execution**: Faster but uses more tokens
- **SQLite**: Use WAL mode for concurrent access
- **Caching**: Consider caching LLM responses for repeated queries

## Security Considerations

- ✅ API keys stored in user secrets (dev) or environment variables (prod)
- ✅ Input validation on all plugin parameters
- ⚠️ No rate limiting implemented yet
- ⚠️ No authentication/authorization for multi-user scenarios

## Troubleshooting

### Common Issues

**Issue**: Multi-agent mode hangs on startup
**Solution**: Check ServiceCollectionExtensions for circular dependencies

**Issue**: SQLite database locked
**Solution**: Ensure WAL mode is enabled, only one writer at a time

**Issue**: Plugin not being called
**Solution**: Check Description attributes, ensure keywords match user intent

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

MIT License - See LICENSE file for details

## Support

- GitHub Issues: [repository-url]/issues
- Documentation: [repository-url]/docs
- Examples: [repository-url]/examples

## Acknowledgments

- Built with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- Inspired by [OpenClaw](https://github.com/openclaw)
- Uses OpenAI GPT models

---

**Version**: 1.0.0  
**Last Updated**: 2026-04-16  
**Maintainer**: Your Name
