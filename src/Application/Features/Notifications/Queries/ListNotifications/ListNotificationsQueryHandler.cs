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
using System.Linq.Expressions;

namespace Application.Features.Notifications.Queries;

public sealed class ListNotificationsQueryHandler : IRequestHandler<ListNotificationsQuery, Result<PagedList<NotificationListItemDto>>>
{
    private readonly IRepository<Notification> _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListNotificationsQueryHandler(IRepository<Notification> notifications, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
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
        Expression<Func<Notification, bool>> predicate =
            n => n.UserId == userId && (!isRead.HasValue || n.IsRead == isRead.Value);

        Expression<Func<Notification, NotificationListItemDto>> selector = n => new NotificationListItemDto(
            n.Id,
            n.Type,
            n.Title,
            n.Message,
            n.Data,
            n.LocalizationKey,
            n.LocalizationParamsJson,
            n.IsRead,
            n.CreatedAt);

        var paged = await _notifications.PagedAsync(
            selector,
            predicate,
            orderBy: n => n.CreatedAt,
            request.ToPagination() with { Descending = true },
            cancellationToken: cancellationToken);

        return Result<PagedList<NotificationListItemDto>>.Success(paged);
    }
}
