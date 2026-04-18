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
        var userId = "anonymous"; // TODO: Extract from authentication when implemented
        var endpoint = context.Request.Path.Value ?? "/";

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
}
