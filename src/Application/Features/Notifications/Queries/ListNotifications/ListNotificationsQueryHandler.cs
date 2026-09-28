using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Notifications;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Queries;

public sealed class ListNotificationsQueryHandler : IRequestHandler<ListNotificationsQuery, Result<PagedList<NotificationListItemDto>>>
{
    private readonly IBaseRepository<Notification> _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListNotificationsQueryHandler(IBaseRepository<Notification> notifications, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    public async Task<Result<PagedList<NotificationListItemDto>>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId<PagedList<NotificationListItemDto>>(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var isRead = request.IsRead;
        System.Linq.Expressions.Expression<Func<Notification, bool>> predicate =
            n => n.UserId == userId && (!isRead.HasValue || n.IsRead == isRead.Value);

        var selector = (System.Linq.Expressions.Expression<Func<Notification, NotificationListItemDto>>)(n => new NotificationListItemDto(
            n.Id,
            n.Type,
            n.Title,
            n.Message,
            n.Data,
            n.LocalizationKey,
            n.LocalizationParamsJson,
            n.IsRead,
            n.CreatedAt));

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(200);

        var totalCount = await _notifications.CountAsync(predicate, cancellationToken);
        var rows = await _notifications.PagedAsync(
            selector, predicate, n => n.CreatedAt, true, page, pageSize, cancellationToken);

        var items = rows.ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<NotificationListItemDto>>.Success(items);
    }
}
