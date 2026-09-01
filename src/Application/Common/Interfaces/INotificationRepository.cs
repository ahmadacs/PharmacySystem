using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using Domain.Entities.Notifications;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<PagedList<NotificationListItemDto>> ListAsync(
        Guid userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Loads and marks all unread notifications for the user as read (tracked).</summary>
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Number of unread notifications for the given user.</summary>
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>True when the user already has an unread notification for the same
    /// (type, data) pair — used to de-duplicate low-stock / near-expiry events.</summary>
    Task<bool> HasUnreadAsync(Guid userId, NotificationType type, string data, CancellationToken cancellationToken = default);
}