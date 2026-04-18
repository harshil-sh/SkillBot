using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Engine;
using SkillBot.Infrastructure.Memory;
using SkillBot.Infrastructure.Plugins;
using SkillBot.Infrastructure.MultiAgent;
using SkillBot.Infrastructure.MultiAgent.Agents;
using SkillBot.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;

namespace SkillBot.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring SkillBot services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSkillBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var options = new SkillBotOptions();
        configuration.GetSection(SkillBotOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        // Add Semantic Kernel
        services.AddSingleton<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

            // Configure AI service (OpenAI or Azure OpenAI)
            if (!string.IsNullOrEmpty(options.AzureEndpoint))
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: options.AzureDeploymentName ?? options.Model,
                    endpoint: options.AzureEndpoint,
                    apiKey: options.ApiKey);
            }
            else
            {
                builder.AddOpenAIChatCompletion(
                    modelId: options.Model,
                    apiKey: options.ApiKey);
            }

            // Add logging
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                if (options.VerboseLogging)
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
            });

            return builder.Build();
        });

        // Register memory provider based on configuration
        RegisterMemoryProvider(services, options);

        // Add caching
        AddCaching(services, options.Caching);

        // Register core services
        services.AddSingleton<IPluginProvider, DynamicPluginProvider>();
        services.AddSingleton<IAgentEngine, SemanticKernelEngine>();

        // Get chat completion service from kernel and wrap with caching if enabled
        services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            var innerService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

            if (options.Caching.Enabled)
            {
                var cacheService = sp.GetRequiredService<ICacheService>();
                var keyBuilder = sp.GetRequiredService<ICacheKeyBuilder>();
                var logger = sp.GetRequiredService<ILogger<CachedChatCompletionService>>();
                return new CachedChatCompletionService(innerService, cacheService, keyBuilder, options.Caching, logger);
            }

            return innerService;
        });

        return services;
    }

    private static void RegisterMemoryProvider(
        IServiceCollection services,
        SkillBotOptions options)
    {
        switch (options.MemoryProvider.ToLowerInvariant())
        {
            case "sqlite":
                services.AddSingleton<IMemoryProvider>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<SqliteMemoryProvider>>();
                    return new SqliteMemoryProvider(options.SqliteDatabasePath, logger);
                });
                break;

            case "inmemory":
            default:
                services.AddSingleton<IMemoryProvider, InMemoryProvider>();
                break;
        }
    }
    
    /// <summary>
    /// Adds multi-agent orchestration capabilities to SkillBot.
    /// FIXED VERSION with proper dependency resolution.
    /// </summary>
    public static IServiceCollection AddMultiAgentOrchestration(
        this IServiceCollection services)
    {
        // Register task router
        services.AddSingleton<ITaskRouter, LlmTaskRouter>();
 
        // Register orchestrator (without auto-registration)
        services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();
 
        // Register specialized agents as singletons
        services.AddSingleton<ResearchAgent>();
        services.AddSingleton<CodingAgent>();
        services.AddSingleton<DataAnalysisAgent>();
        services.AddSingleton<WritingAgent>();
 
        // Register them as ISpecializedAgent for collection
        services.AddSingleton<ISpecializedAgent>(sp => sp.GetRequiredService<ResearchAgent>());
        services.AddSingleton<ISpecializedAgent>(sp => sp.GetRequiredService<CodingAgent>());
        services.AddSingleton<ISpecializedAgent>(sp => sp.GetRequiredService<DataAnalysisAgent>());
        services.AddSingleton<ISpecializedAgent>(sp => sp.GetRequiredService<WritingAgent>());
 
        // IMPORTANT: Use a factory to register agents AFTER orchestrator is created
        services.AddSingleton<IAgentOrchestrator>(sp =>
        {
            var taskRouter = sp.GetRequiredService<ITaskRouter>();
            var logger = sp.GetRequiredService<ILogger<AgentOrchestrator>>();
            var orchestrator = new AgentOrchestrator(taskRouter, logger);
 
            // Register all agents
            var researchAgent = sp.GetRequiredService<ResearchAgent>();
            var codingAgent = sp.GetRequiredService<CodingAgent>();
            var dataAgent = sp.GetRequiredService<DataAnalysisAgent>();
            var writingAgent = sp.GetRequiredService<WritingAgent>();
 
            orchestrator.RegisterAgent(researchAgent);
            orchestrator.RegisterAgent(codingAgent);
            orchestrator.RegisterAgent(dataAgent);
            orchestrator.RegisterAgent(writingAgent);
 
            return orchestrator;
        });
 
        return services;
    }

    private static void AddCaching(
        IServiceCollection services,
        CachingOptions cachingOptions)
    {
        if (!cachingOptions.Enabled)
            return;

        // Register memory cache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = cachingOptions.MemoryCacheSizeMb * 1024 * 1024;
        });

        // Register cache services
        services.AddSingleton(cachingOptions);
        services.AddSingleton<ICacheKeyBuilder, CacheKeyBuilder>();
        services.AddSingleton<ICacheService, HybridCacheService>();
        services.AddSingleton<ICacheManagementService, CacheManagementService>();

        // Register background cleanup service if enabled
        if (cachingOptions.EnableAutoCleanup)
        {
            services.AddHostedService<CacheCleanupBackgroundService>();
        }
    }
}

