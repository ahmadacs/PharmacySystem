using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.HealthChecks;

/// <summary>
/// Lightweight Redis liveness probe over raw RESP (PING [+ AUTH]).
/// Zero extra NuGet dependencies: parses the same StackExchange-style
/// "host:port,password=...,abortConnect=false" connection string used
/// by AddStackExchangeRedisOutputCache. Degraded (not unhealthy) when
/// Redis is unconfigured, because the app falls back to in-memory cache.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly string? _connectionString;

    public RedisHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Redis");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            return HealthCheckResult.Degraded("Redis connection string is not configured; using in-memory output cache.");

        if (!TryParse(_connectionString, out var host, out var port, out var password))
            return HealthCheckResult.Unhealthy($"Unparsable Redis connection string.");

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, timeoutCts.Token);
            await using var stream = client.GetStream();

            if (!string.IsNullOrEmpty(password))
            {
                var authReply = await SendCommandAsync(stream, $"AUTH {password}", timeoutCts.Token);
                if (!authReply.StartsWith("+OK", StringComparison.Ordinal))
                    return HealthCheckResult.Unhealthy("Redis AUTH failed.");
            }

            var pingReply = await SendCommandAsync(stream, "PING", timeoutCts.Token);
            return pingReply.Contains("PONG", StringComparison.Ordinal)
                ? HealthCheckResult.Healthy("Redis PING returned PONG.")
                : HealthCheckResult.Unhealthy($"Unexpected Redis PING reply: {pingReply.Trim()}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", ex);
        }
    }

    private static async Task<string> SendCommandAsync(NetworkStream stream, string command, CancellationToken ct)
    {
        // Inline command + CRLF is sufficient for AUTH/PING against redis-server.
        var payload = Encoding.ASCII.GetBytes(command + "\r\n");
        await stream.WriteAsync(payload, ct);
        var buffer = new byte[256];
        var read = await stream.ReadAsync(buffer, ct);
        return Encoding.ASCII.GetString(buffer, 0, read);
    }

    internal static bool TryParse(string connectionString, out string host, out int port, out string? password)
    {
        host = "localhost";
        port = 6379;
        password = null;

        var firstSegment = connectionString.Split(',')[0].Trim();
        // Strip optional scheme (redis://) if ever used.
        const string scheme = "redis://";
        if (firstSegment.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            firstSegment = firstSegment[scheme.Length..];

        // Drop trailing DB index ("/0") if present.
        var slash = firstSegment.IndexOf('/');
        if (slash >= 0)
            firstSegment = firstSegment[..slash];

        var hostPort = firstSegment.Split(':');
        if (hostPort.Length == 0 || string.IsNullOrWhiteSpace(hostPort[0]))
            return false;
        host = hostPort[0].Trim();
        if (hostPort.Length > 1 && (!int.TryParse(hostPort[1].Trim(), out port) || port is <= 0 or > 65535))
            return false;

        foreach (var part in connectionString.Split(',').Skip(1))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase))
                password = kv[1].Trim();
        }

        return true;
    }
}
