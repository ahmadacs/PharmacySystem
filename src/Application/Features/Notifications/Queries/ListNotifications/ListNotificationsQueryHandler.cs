using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Notifications.Queries;

public sealed class ListNotificationsQueryHandler : IRequestHandler<ListNotificationsQuery, Result<PagedList<NotificationListItemDto>>>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IAsyncQueryExecutor _executor;

    public ListNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUser, IAsyncQueryExecutor executor)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _executor = executor;
    }

    public async Task<Result<PagedList<NotificationListItemDto>>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var query = _notifications.Query().Where(n => n.UserId == userId);
            if (request.IsRead.HasValue)
                query = query.Where(n => n.IsRead == request.IsRead.Value);

            var ordered = query.OrderByDescending(n => n.CreatedAt);

            var totalCount = await _executor.CountAsync(ordered, cancellationToken);

            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 200);

            var rows = await _executor.ToListAsync(
                ordered.Skip((page - 1) * pageSize).Take(pageSize),
                cancellationToken);

            var items = rows
                .Select(n => n.ToListItemDto())
                .ToPagedList(page, pageSize, totalCount);

            return Result<PagedList<NotificationListItemDto>>.Success(items);
        }

        return Result<PagedList<NotificationListItemDto>>.Failure(authResult.Error!, 403);
    }
}
