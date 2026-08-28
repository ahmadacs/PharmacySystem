namespace Application.Common.Options;

/// <summary>
/// Notification behavior settings. Bound from the "Notifications" section in
/// appsettings.json by the Infrastructure DI; Application handlers inject the
/// plain POCO (no framework dependency needed).
/// </summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Batches expiring within this many days are considered near-expiry.</summary>
    public int ExpiryWarningDays { get; init; } = 30;
}