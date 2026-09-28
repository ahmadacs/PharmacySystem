namespace Application.Common.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public int ExpiryWarningDays { get; init; } = 30;
}