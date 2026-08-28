using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace WebApi.Middleware;

public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;
    private readonly int _thresholdMs;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _thresholdMs = config.GetValue<int?>("Diagnostics:SlowRequestThresholdMs") ?? 1000;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var ms = sw.ElapsedMilliseconds;
        if (ms >= _thresholdMs)
        {
            _logger.LogWarning("Slow request {Method} {Path} responded {StatusCode} in {Elapsed}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, ms);
        }
        else
        {
            _logger.LogDebug("Request {Method} {Path} responded {StatusCode} in {Elapsed}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, ms);
        }
    }
}
