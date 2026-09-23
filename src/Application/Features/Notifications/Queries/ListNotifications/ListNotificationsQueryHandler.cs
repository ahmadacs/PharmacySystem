using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
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

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(200);

        var spec = new Specification<Notification, Notification>(n => n);
        spec.Where(n => n.UserId == userId);
        if (request.IsRead.HasValue)
            spec.Where(n => n.IsRead == request.IsRead.Value);
        spec.Order(q => q.OrderByDescending(n => n.CreatedAt));

        var totalCount = await _notifications.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _notifications.ListAsync(spec, cancellationToken);

        var items = rows
            .Select(n => n.ToListItemDto())
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<NotificationListItemDto>>.Success(items);
    }
}
