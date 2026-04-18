using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Serilog;
using Hangfire;
using Hangfire.MemoryStorage;
using SkillBot.Api.Middleware;
using SkillBot.Api.Services;
using SkillBot.Infrastructure.Configuration;
using SkillBot.Plugins.Examples;
using SkillBot.Plugins.OpenAI;
using SkillBot.Plugins.Search;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkillBot API",
        Version = "v1.0.0",
        Description = @"AI Agent Framework with Multi-Agent Orchestration, Web Search, Token Tracking, and Background Tasks

Features:
- Single & Multi-Agent chat
- Web search integration
- Token usage tracking
- Background task scheduling
- Plugin management",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com",
            Url = new Uri("https://github.com/yourusername/skillbot")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',') 
                ?? new[] { "http://localhost:5000", "https://localhost:5001" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Hangfire for background tasks
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()); // For production, use SQL Server or PostgreSQL

builder.Services.AddHangfireServer();

// Add SkillBot services
builder.Services.AddSkillBot(builder.Configuration);
builder.Services.AddMultiAgentOrchestration();

// Add API-specific services
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddSingleton<ITokenUsageService, TokenUsageService>();
builder.Services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();

builder.Services.AddMemoryCache(); // For conversation & usage caching
builder.Services.AddHttpContextAccessor(); // For future user context

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<SkillBotHealthCheck>("skillbot_health");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillBot API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
        options.DocumentTitle = "SkillBot API Documentation";
    });
}

// Middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // TODO: Add authentication for production
    // Authorization = new[] { new MyAuthorizationFilter() }
});

app.UseHttpsRedirection();
app.UseCors("AllowBlazorApp");

// TODO: Add authentication middleware here (Phase 2)
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Register plugins on startup
using (var scope = app.Services.CreateScope())
{
    var pluginProvider = scope.ServiceProvider.GetRequiredService<SkillBot.Core.Interfaces.IPluginProvider>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Registering plugins...");
    
    try
    {
        // Core plugins
        pluginProvider.RegisterPlugin(new CalculatorPlugin());
        pluginProvider.RegisterPlugin(new WeatherPlugin());
        pluginProvider.RegisterPlugin(new TimePlugin());
        
        // OpenAI usage plugin
        pluginProvider.RegisterPlugin(new SimpleUsagePlugin(builder.Configuration));
        
        // Web search plugin (if configured)
        var serpApiKey = builder.Configuration["SerpApi:ApiKey"];

        if (!string.IsNullOrEmpty(serpApiKey))
        {
            var serpApiPlugin = new SerpApiPlugin(
                builder.Configuration,
                scope.ServiceProvider.GetRequiredService<ILogger<SerpApiPlugin>>());

            // Wrap with caching if enabled
            var cachingOptions = scope.ServiceProvider.GetRequiredService<SkillBotOptions>().Caching;
            if (cachingOptions.Enabled)
            {
                var cacheService = scope.ServiceProvider.GetRequiredService<SkillBot.Core.Interfaces.ICacheService>();
                var keyBuilder = scope.ServiceProvider.GetRequiredService<SkillBot.Core.Interfaces.ICacheKeyBuilder>();
                var cachedPlugin = new CachedSerpApiPlugin(
                    serpApiPlugin,
                    cacheService,
                    keyBuilder,
                    cachingOptions,
                    scope.ServiceProvider.GetRequiredService<ILogger<CachedSerpApiPlugin>>());

                pluginProvider.RegisterPlugin(cachedPlugin);
                logger.LogInformation("SerpAPI search plugin registered with caching");
            }
            else
            {
                pluginProvider.RegisterPlugin(serpApiPlugin);
                logger.LogInformation("SerpAPI search plugin registered");
            }
        }
        else
        {
            logger.LogWarning("SerpAPI key not configured. Web search plugin not available.");
        }
        
        logger.LogInformation("Successfully registered {Count} plugins", 
            pluginProvider.GetRegisteredPlugins().Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to register plugins");
    }
}

Log.Information("SkillBot API starting...");
Log.Information("Hangfire Dashboard available at: /hangfire");

app.Lifetime.ApplicationStarted.Register(() =>
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

    foreach (var url in app.Urls)
    {
        startupLogger.LogInformation("Now listening on: {Url}", url);
    }

    var firstUrl = app.Urls.FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(firstUrl))
    {
        startupLogger.LogInformation("Swagger UI available at: {SwaggerUrl}", firstUrl.TrimEnd('/') + "/");
        startupLogger.LogInformation("Hangfire Dashboard available at: {HangfireUrl}", firstUrl.TrimEnd('/') + "/hangfire");
    }
});

app.Run();
Log.Information("SkillBot API stopped");

// Health check implementation
public class SkillBotHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly SkillBot.Core.Interfaces.IAgentEngine _engine;
    
    public SkillBotHealthCheck(SkillBot.Core.Interfaces.IAgentEngine engine)
    {
        _engine = engine;
    }
    
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = _engine.Context != null;
            
            if (isHealthy)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                    "SkillBot API is healthy");
            }
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "SkillBot engine not initialized");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "SkillBot health check failed", ex);
        }
    }
}
