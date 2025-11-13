using System.Diagnostics;

namespace YurtCord.API.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with timing information
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var traceId = context.TraceIdentifier;

        _logger.LogInformation(
            "[{TraceId}] {Method} {Path} - Started",
            traceId,
            requestMethod,
            requestPath
        );

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;

            _logger.Log(
                logLevel,
                "[{TraceId}] {Method} {Path} - {StatusCode} - {Duration}ms",
                traceId,
                requestMethod,
                requestPath,
                statusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}

/// <summary>
/// Extension method to register the request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
