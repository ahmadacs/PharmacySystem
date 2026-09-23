using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.HealthChecks;

/// <summary>
/// Redis liveness probe: TCP connect + PING (with AUTH when a password is set).
/// Degraded when Redis is unconfigured because the app falls back to in-memory cache.
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
            return HealthCheckResult.Degraded("Redis is not configured; using in-memory output cache.");

        if (!TryParse(_connectionString, out var host, out var port, out var password))
            return HealthCheckResult.Unhealthy("Unparsable Redis connection string.");

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, timeoutCts.Token);
            await using var stream = client.GetStream();

            if (!string.IsNullOrEmpty(password))
            {
                var authReply = await SendAsync(stream, $"AUTH {password}", timeoutCts.Token);
                if (!authReply.StartsWith("+OK", StringComparison.Ordinal))
                    return HealthCheckResult.Unhealthy("Redis AUTH failed.");
            }

            var pingReply = await SendAsync(stream, "PING", timeoutCts.Token);
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

    private static async Task<string> SendAsync(NetworkStream stream, string command, CancellationToken ct)
    {
        var payload = Encoding.ASCII.GetBytes(command + "\r\n");
        await stream.WriteAsync(payload, ct);
        var buffer = new byte[256];
        var read = await stream.ReadAsync(buffer, ct);
        return Encoding.ASCII.GetString(buffer, 0, read);
    }

    private static bool TryParse(string connectionString, out string host, out int port, out string? password)
    {
        host = "localhost";
        port = 6379;
        password = null;

        var segments = connectionString.Split(',');
        var hostPort = segments[0].Trim().Split(':');
        if (string.IsNullOrWhiteSpace(hostPort[0]))
            return false;
        host = hostPort[0].Trim();
        if (hostPort.Length > 1 && (!int.TryParse(hostPort[1].Trim(), out port) || port is <= 0 or > 65535))
            return false;

        foreach (var part in segments.Skip(1))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase))
                password = kv[1].Trim();
        }

        return true;
    }
}
