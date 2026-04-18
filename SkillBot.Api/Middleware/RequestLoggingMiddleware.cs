using System.Diagnostics;

namespace SkillBot.Api.Middleware;

/// <summary>
/// Logs all incoming requests and their execution time
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        try
        {
            _logger.LogInformation(
                "Incoming {Method} request to {Path}",
                requestMethod,
                requestPath);

            await _next(context);

            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            _logger.LogInformation(
                "Completed {Method} {Path} with {StatusCode} in {ElapsedMs}ms",
                requestMethod,
                requestPath,
                statusCode,
                elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            
            _logger.LogError(
                ex,
                "Failed {Method} {Path} after {ElapsedMs}ms",
                requestMethod,
                requestPath,
                elapsed);

            throw;
        }
    }
}
