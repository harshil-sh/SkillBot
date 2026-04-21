using SkillBot.Api.Models.Responses;
using SkillBot.Api.Services;
using System.Text.Json;

namespace SkillBot.Api.Middleware;

public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimiter rateLimiter)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip rate limiting for health checks, swagger, and static/framework files
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        var userId = "anonymous"; // TODO: Extract from authentication when implemented
        var endpoint = path;

        var rateLimitResult = await rateLimiter.CheckRateLimitAsync(userId, endpoint);

        if (!rateLimitResult.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId} on {Endpoint}", userId, endpoint);

            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = ((int)rateLimitResult.RetryAfter.TotalSeconds).ToString();

            var errorResponse = new ErrorResponse
            {
                Error = "RateLimitExceeded",
                Message = $"Rate limit exceeded. Retry after {rateLimitResult.RetryAfter.TotalSeconds:F0} seconds"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        await _next(context);
    }

    private static bool IsExemptPath(string path)
    {
        // Health checks, swagger UI, Blazor framework/content, and static files
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase))
            return true;

        // Static file extensions
        var ext = Path.GetExtension(path);
        return ext is ".js" or ".css" or ".wasm" or ".dll" or ".dat" or ".blat" or
                       ".png" or ".ico" or ".svg" or ".webmanifest" or ".json" or
                       ".html" or ".htm" or ".map" or ".gz" or ".br";
    }
}
