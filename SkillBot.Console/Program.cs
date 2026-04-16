using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Configuration;
using SkillBot.Plugins.Examples;
using SkillBot.Plugins.OpenAI;

namespace SkillBot.Console;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Check if multi-agent mode is requested
            var useMultiAgent = args.Contains("--multi-agent") || args.Contains("-m");

            System.Console.WriteLine("🚀 Starting SkillBot...");
            
            if (useMultiAgent)
            {
                System.Console.WriteLine("📡 Multi-Agent Mode - This may take a moment...");
            }

            // Build host with configuration and DI
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    System.Console.WriteLine("⚙️  Loading configuration...");
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddEnvironmentVariables();
                    config.AddUserSecrets<Program>(optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    System.Console.WriteLine("🔧 Registering services...");
                    
                    // Register SkillBot services
                    services.AddSkillBot(context.Configuration);

                    // Register multi-agent orchestration if requested
                    if (useMultiAgent)
                    {
                        System.Console.WriteLine("🤖 Initializing multi-agent system...");
                        services.AddMultiAgentOrchestration();
                    }
                    
                    System.Console.WriteLine("✅ Services registered");
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            System.Console.WriteLine("✅ SkillBot initialized successfully\n");

            // Run the appropriate mode
            if (useMultiAgent)
            {
                await RunMultiAgentAsync(host.Services);
            }
            else
            {
                await RunSingleAgentAsync(host.Services);
            }
        }
        catch (Exception ex)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");
            System.Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
            System.Console.ResetColor();
            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();
        }
    }

    static async Task RunSingleAgentAsync(IServiceProvider services)
    {
        var engine = services.GetRequiredService<IAgentEngine>();
        var pluginProvider = services.GetRequiredService<IPluginProvider>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        System.Console.WriteLine("╔════════════════════════════════════════╗");
        System.Console.WriteLine("║         SkillBot v1.0                  ║");
        System.Console.WriteLine("║  AI Agentic Runtime with .NET & SK     ║");
        System.Console.WriteLine("╚════════════════════════════════════════╝");
        System.Console.WriteLine();

        // Register built-in plugins
        System.Console.WriteLine("Registering plugins...");
        pluginProvider.RegisterPlugin(new CalculatorPlugin());
        pluginProvider.RegisterPlugin(new WeatherPlugin());
        pluginProvider.RegisterPlugin(new TimePlugin());

        // Add OpenAI monitoring
        var configuration = services.GetRequiredService<IConfiguration>();
        pluginProvider.RegisterPlugin(new OpenAIUsagePlugin(configuration));

        var plugins = pluginProvider.GetRegisteredPlugins();
        System.Console.WriteLine($"✓ Loaded {plugins.Count} plugins:");
        foreach (var plugin in plugins)
        {
            System.Console.WriteLine($"  • {plugin.Name}: {plugin.Description}");
        }
        System.Console.WriteLine();
        System.Console.WriteLine("Type your message (or 'quit' to exit, 'reset' to clear history):");
        System.Console.WriteLine("To use multi-agent mode, restart with: dotnet run -- --multi-agent");
        System.Console.WriteLine(new string('─', 50));
        System.Console.WriteLine();

        await RunAgentLoopAsync(engine);
    }

    static async Task RunMultiAgentAsync(IServiceProvider services)
    {
        try
        {
            System.Console.WriteLine("🔍 Getting orchestrator service...");
            var orchestrator = services.GetRequiredService<IAgentOrchestrator>();
            
            System.Console.WriteLine("🔍 Getting plugin provider...");
            var pluginProvider = services.GetRequiredService<IPluginProvider>();
            
            var logger = services.GetRequiredService<ILogger<Program>>();

            System.Console.Clear();
            System.Console.WriteLine("╔════════════════════════════════════════╗");
            System.Console.WriteLine("║      SkillBot v1.0 - MULTI-AGENT      ║");
            System.Console.WriteLine("║   Multiple AI Specialists Working      ║");
            System.Console.WriteLine("║            Together                    ║");
            System.Console.WriteLine("╚════════════════════════════════════════╝");
            System.Console.WriteLine();

            // Register built-in plugins for all agents
            System.Console.WriteLine("Registering shared plugins...");
            pluginProvider.RegisterPlugin(new CalculatorPlugin());
            pluginProvider.RegisterPlugin(new WeatherPlugin());
            pluginProvider.RegisterPlugin(new TimePlugin());
            // Add OpenAI monitoring
            var configuration = services.GetRequiredService<IConfiguration>();
            pluginProvider.RegisterPlugin(new OpenAIUsagePlugin(configuration));
            System.Console.WriteLine("✓ Plugins registered");

            // Show registered agents
            System.Console.WriteLine("\nLoading specialist agents...");
            var agents = orchestrator.GetAgents();
            System.Console.WriteLine($"✓ Multi-Agent System Active with {agents.Count} specialists:\n");
            
            foreach (var agent in agents)
            {
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine($"  • {agent.Name}");
                System.Console.ResetColor();
                System.Console.WriteLine($"    {agent.Description}");
                System.Console.WriteLine($"    Specializations: {string.Join(", ", agent.Specializations)}");
                System.Console.WriteLine();
            }

            System.Console.WriteLine("Type your message (or 'quit' to exit, 'status' for agent status):");
            System.Console.WriteLine(new string('─', 50));
            System.Console.WriteLine();

            await RunOrchestratorLoopAsync(orchestrator);
        }
        catch (Exception ex)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\n❌ Error initializing multi-agent system: {ex.Message}");
            System.Console.WriteLine($"\nDetails: {ex.InnerException?.Message ?? "No additional details"}");
            System.Console.ResetColor();
            throw;
        }
    }

    static async Task RunAgentLoopAsync(IAgentEngine engine)
    {
        while (true)
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.Write("You: ");
            System.Console.ResetColor();

            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("\nGoodbye! 👋");
                break;
            }

            if (input.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                await engine.ResetAsync();
                System.Console.WriteLine("✓ Conversation history cleared.\n");
                continue;
            }

            try
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write("Assistant: ");
                System.Console.ResetColor();

                var response = await engine.ExecuteAsync(input);

                if (response.IsSuccess)
                {
                    System.Console.WriteLine(response.Content);

                    if (response.ToolCalls.Count > 0)
                    {
                        System.Console.ForegroundColor = ConsoleColor.DarkGray;
                        System.Console.WriteLine($"\n[Used {response.ToolCalls.Count} tool(s) in {response.ExecutionTime.TotalMilliseconds:F0}ms]");
                        System.Console.ResetColor();
                    }
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"Error: {response.ErrorMessage}");
                    System.Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine();
        }
    }

    static async Task RunOrchestratorLoopAsync(IAgentOrchestrator orchestrator)
    {
        while (true)
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.Write("You: ");
            System.Console.ResetColor();

            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("\nGoodbye! 👋");
                break;
            }

            if (input.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                ShowAgentStatus(orchestrator);
                continue;
            }

            try
            {
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine("🤖 Coordinating specialists...");
                System.Console.ResetColor();

                var response = await orchestrator.ExecuteTaskAsync(input);

                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.Write("\nOrchestrator: ");
                System.Console.ResetColor();
                System.Console.WriteLine(response.FinalResponse);

                if (response.AgentResults.Count > 0)
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkGray;
                    System.Console.WriteLine($"\n[Coordinated {response.AgentResults.Count} agent(s) in {response.TotalExecutionTime.TotalMilliseconds:F0}ms]");
                    
                    if (response.Metadata != null && response.Metadata.ContainsKey("RoutingStrategy"))
                    {
                        System.Console.WriteLine($"Strategy: {response.Metadata["RoutingStrategy"]}");
                    }
                    
                    System.Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"Error: {ex.Message}");
                System.Console.WriteLine($"Details: {ex.InnerException?.Message}");
                System.Console.ResetColor();
            }

            System.Console.WriteLine();
        }
    }

    static void ShowAgentStatus(IAgentOrchestrator orchestrator)
    {
        var agents = orchestrator.GetAgents();

        System.Console.WriteLine("\n╔════════════════════════════════════════╗");
        System.Console.WriteLine("║         Agent Status Report            ║");
        System.Console.WriteLine("╚════════════════════════════════════════╝\n");

        foreach (var agent in agents)
        {
            var status = agent.GetStatus();

            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"• {agent.Name} ({status.State})");
            System.Console.ResetColor();
            System.Console.WriteLine($"  Tasks Completed: {status.TasksCompleted}");
            System.Console.WriteLine($"  Tasks Failed: {status.TasksFailed}");
            System.Console.WriteLine($"  Last Active: {status.LastActive:HH:mm:ss}");
            System.Console.WriteLine();
        }
    }
}