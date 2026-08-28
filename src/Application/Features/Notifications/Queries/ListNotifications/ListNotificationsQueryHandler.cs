using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Notifications.Queries;

public sealed class ListNotificationsQueryHandler : IRequestHandler<ListNotificationsQuery, PagedResult<NotificationListItemDto>>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;

    public ListNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public Task<PagedResult<NotificationListItemDto>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        return _notifications.ListAsync(userId, request.IsRead, request.Page, request.PageSize, cancellationToken);
    }
}