using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviours;

/// <summary>
/// Logs every MediatR request with a structured trace id so failures can be
/// correlated with the Serilog request log.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var traceId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        _logger.LogInformation("Handling {RequestName} [TraceId: {TraceId}]", typeof(TRequest).Name, traceId);

        try
        {
            var response = await next().ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request {RequestName} failed [TraceId: {TraceId}]", typeof(TRequest).Name, traceId);
            throw;
        }
    }
}