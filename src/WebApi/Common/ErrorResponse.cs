using System.Diagnostics;

namespace WebApi.Common;

/// <summary>
/// Consistent error envelope returned by every failing request. Field-level
/// validation messages go in <c>errors</c>; general failures in <c>message</c>.
/// </summary>
public sealed class ErrorResponse
{
    public bool Success { get; } = false;
    public string? Message { get; init; }
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
    public string TraceId { get; init; } = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
}