using Domain.Entities.Notifications;
using Domain.Enums;

namespace Application.Features.Notifications.Dtos;

public sealed record NotificationListItemDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? Data,
    string? LocalizationKey,
    string? LocalizationParamsJson,
    bool IsRead,
    DateTime CreatedAt);

public static class NotificationMapping
{
    public static NotificationListItemDto ToListItemDto(this Notification notification)
        => new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.Data,
            notification.LocalizationKey,
            notification.LocalizationParamsJson,
            notification.IsRead,
            notification.CreatedAt);
}