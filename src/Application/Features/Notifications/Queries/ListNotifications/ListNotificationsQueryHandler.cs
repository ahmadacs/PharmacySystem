using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Queries;

public sealed class ListNotificationsQueryHandler : IRequestHandler<ListNotificationsQuery, Result<PagedList<NotificationListItemDto>>>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUser, IAsyncQueryExecutor executor, IStringLocalizer<SharedResource> localizer)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _executor = executor;
        _localizer = localizer;
    }

    public async Task<Result<PagedList<NotificationListItemDto>>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId<PagedList<NotificationListItemDto>>(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var query = _notifications.Query().Where(n => n.UserId == userId);
        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var ordered = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await _executor.CountAsync(ordered, cancellationToken);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(200);

        var rows = await _executor.ToListAsync(
            ordered.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken);

        var items = rows
            .Select(n => n.ToListItemDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<NotificationListItemDto>>.Success(items);
    }
}
