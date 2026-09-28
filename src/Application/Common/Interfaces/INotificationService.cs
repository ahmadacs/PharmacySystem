using Domain.Enums;

namespace Application.Common.Interfaces;

public sealed record NotificationCreate(
    NotificationType Type,
    string Title,
    string Message,
    string? Data = null,
    string? LocalizationKey = null,
    string? LocalizationParamsJson = null);

public interface INotificationService
{
    Task SendToUserAsync(Guid userId, NotificationCreate notification, CancellationToken cancellationToken = default);

    Task SendToRoleAsync(string role, NotificationCreate notification, CancellationToken cancellationToken = default);
}