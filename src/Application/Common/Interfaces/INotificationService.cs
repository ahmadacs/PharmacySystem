using Domain.Enums;

namespace Application.Common.Interfaces;

/// <summary>Payload for a notification push. Data is a small JSON string used for
/// navigation and de-duplication (see Infrastructure.NotificationService).</summary>
public sealed record NotificationCreate(
    NotificationType Type,
    string Title,
    string Message,
    string? Data = null,
    string? LocalizationKey = null,
    string? LocalizationParamsJson = null);

/// <summary>
/// Persists a notification for a user and pushes it over SignalR. Implemented in
/// Infrastructure (NotificationService) which owns the hub connection.
/// </summary>
public interface INotificationService
{
    Task SendToUserAsync(Guid userId, NotificationCreate notification, CancellationToken cancellationToken = default);

    /// <summary>Sends to every active user holding the given role.</summary>
    Task SendToRoleAsync(string role, NotificationCreate notification, CancellationToken cancellationToken = default);
}