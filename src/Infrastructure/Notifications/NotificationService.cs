using Application.Common.Interfaces;
using Domain.Entities.Notifications;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBaseRepository<Notification> _notifications;

    public NotificationService(
        ApplicationDbContext db,
        IHubContext<NotificationsHub> hub,
        UserManager<ApplicationUser> userManager,
        IBaseRepository<Notification> notifications)
    {
        _db = db;
        _hub = hub;
        _userManager = userManager;
        _notifications = notifications;
    }

    public async Task SendToUserAsync(Guid userId, NotificationCreate notification, CancellationToken cancellationToken = default)
    {
        if (await ShouldSkipAsync(userId, notification, cancellationToken))
            return;

        var entity = new Notification(
            userId, notification.Type, notification.Title, notification.Message, notification.Data, notification.LocalizationKey, notification.LocalizationParamsJson);

        _db.Set<Notification>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Group($"user:{userId}").SendAsync("notification", ToPayload(entity), cancellationToken);
    }

    public async Task SendToRoleAsync(string role, NotificationCreate notification, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);

        foreach (var user in users)
        {
            if (!user.IsActive)
                continue;
            await SendToUserAsync(user.Id, notification, cancellationToken);
        }
    }

    private async Task<bool> ShouldSkipAsync(Guid userId, NotificationCreate notification, CancellationToken cancellationToken)
    {
        if (notification.Data is null)
            return false;
        if (notification.Type is not (NotificationType.LowStock or NotificationType.NearExpiry))
            return false;

        return await _notifications.ExistsAsync(n => n.UserId == userId && n.Type == notification.Type && n.Data == notification.Data && !n.IsRead, cancellationToken);
    }

    private static object ToPayload(Notification entity) => new
    {
        id = entity.Id,
        type = entity.Type.ToString(),
        title = entity.Title,
        message = entity.Message,
        data = entity.Data,
        localizationKey = entity.LocalizationKey,
        localizationParamsJson = entity.LocalizationParamsJson,
        isRead = entity.IsRead,
        createdAt = entity.CreatedAt
    };
}