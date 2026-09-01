using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Notifications.Dtos;
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

    public async Task<PagedList<NotificationListItemDto>> ListAsync(
        Guid userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Notification> data = Db.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
            data = data.Where(n => n.IsRead == isRead.Value);

        var totalCount = await data.CountAsync(cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var items = await data
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemDto(
                n.Id, n.Type, n.Title, n.Message, n.Data, n.LocalizationKey, n.LocalizationParamsJson, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedList<NotificationListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await Db.Set<Notification>()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
            notification.MarkRead(DateTime.UtcNow);

        return unread.Count;
    }

    public Task<bool> HasUnreadAsync(Guid userId, NotificationType type, string data, CancellationToken cancellationToken = default)
        => Db.Set<Notification>().AnyAsync(
            n => n.UserId == userId && n.Type == type && n.Data == data && !n.IsRead,
            cancellationToken);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        => Db.Set<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
}