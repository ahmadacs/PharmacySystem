using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Notifications;

public class Notification : IHardDelete
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public string? Data { get; private set; }
    public string? LocalizationKey { get; private set; }
    public string? LocalizationParamsJson { get; private set; }

    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification() { }

    public Notification(Guid userId, NotificationType type, string title, string message, string? data = null, string? localizationKey = null, string? localizationParamsJson = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        UserId = userId;
        Type = type;
        Title = title.Trim();
        Message = message.Trim();
        Data = data;
        LocalizationKey = localizationKey;
        LocalizationParamsJson = localizationParamsJson;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkRead(DateTime at)
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = at;
    }
}