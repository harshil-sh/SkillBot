using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillBot.Console.Services;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Configuration;
using SkillBot.Plugins.Examples;
using SkillBot.Plugins.OpenAI;
using System.IO;

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

                    var basePath = Directory.GetCurrentDirectory();
                    if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
                    {
                        var projectPath = Path.Combine(basePath, "SkillBot.Console");
                        if (File.Exists(Path.Combine(projectPath, "appsettings.json")))
                        {
                            basePath = projectPath;
                        }
                    }

                    config.SetBasePath(basePath);
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddEnvironmentVariables();
                    config.AddUserSecrets<Program>(optional: true);
                })
                .ConfigureServices((context, services) =>
                {
                    System.Console.WriteLine("🔧 Registering services...");
                    
                    // Register console services
                    services.AddHttpClient();
                    services.AddSingleton<ApiClient>(sp =>
                    {
                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        return new ApiClient(httpClientFactory.CreateClient(), configuration);
                    });
                    services.AddSingleton<ICommandParser, CommandParser>();
                    services.AddSingleton<IConsoleAuthService, ConsoleAuthService>();
                    services.AddSingleton<IConsoleChatService, ConsoleChatService>();
                    services.AddSingleton<IConsoleSearchService, ConsoleSearchService>();
                    services.AddSingleton<IConsoleSettingsService, ConsoleSettingsService>();
                    services.AddSingleton<IConsoleAdminService, ConsoleAdminService>();
                    services.AddSingleton<IConsolePluginService, ConsolePluginService>();
                    services.AddSingleton<IConsoleTaskService, ConsoleTaskService>();
                    services.AddSingleton<CommandRouter>();

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
        System.Console.WriteLine("Type a command or 'help' for available commands. Type 'exit' to quit.");
        System.Console.WriteLine("Tip: once logged in, plain text is treated as a chat message (no 'chat' prefix needed).");
        System.Console.WriteLine(new string('─', 50));
        System.Console.WriteLine();

        await RunAgentLoopAsync(services);
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

    static async Task RunAgentLoopAsync(IServiceProvider serviceProvider)
    {
        var parser = serviceProvider.GetRequiredService<ICommandParser>();
        var authService = serviceProvider.GetRequiredService<IConsoleAuthService>();
        var router = new CommandRouter();
        var knownCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "register", "login", "logout",
            "chat", "multi-agent", "history", "conversation", "delete-conversation",
            "search", "search-news",
            "settings",
            "plugins",
            "agents",
            "tasks",
            "stats", "stats-conversation", "top-conversations", "reset-stats",
            "cache-stats", "cache-health", "cache-clear", "cache-invalidate",
            "users", "health",
            "help", "exit"
        };

        while (true)
        {
            System.Console.Write("> ");
            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;

            CommandResult commandResult;
            var firstToken = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var shouldTreatAsChat = !string.IsNullOrWhiteSpace(firstToken)
                                    && !knownCommands.Contains(firstToken)
                                    && !string.IsNullOrWhiteSpace(authService.GetCurrentToken());

            if (shouldTreatAsChat)
            {
                commandResult = new CommandResult
                {
                    Command = "chat",
                    IsValid = true,
                    Arguments = new Dictionary<string, string> { ["0"] = input }
                };
            }
            else
            {
                commandResult = await parser.ParseAsync(input);
            }

            if (!commandResult.IsValid)
            {
                System.Console.WriteLine($"Error: {commandResult.ErrorMessage}");
                continue;
            }

            if (commandResult.Command == "exit") break;

            try
            {
                await router.ExecuteAsync(commandResult, serviceProvider);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
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