using System.Diagnostics;

namespace WebApi.Common;

public sealed class ErrorResponse
{
    public bool Success { get; } = false;
    public string? Message { get; init; }
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
    public string TraceId { get; init; } = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
}