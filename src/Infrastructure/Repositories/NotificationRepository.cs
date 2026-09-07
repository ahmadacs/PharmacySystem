using Application.Common.Interfaces;
using Domain.Entities.Notifications;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext db) : base(db)
    {
    }

    public Task<bool> HasUnreadAsync(Guid userId, NotificationType type, string data, CancellationToken cancellationToken = default)
        => Db.Set<Notification>().AnyAsync(
            n => n.UserId == userId && n.Type == type && n.Data == data && !n.IsRead,
            cancellationToken);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        => Db.Set<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
}