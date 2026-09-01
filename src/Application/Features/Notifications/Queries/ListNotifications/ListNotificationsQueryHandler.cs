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

    public ListNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<NotificationListItemDto>>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;
            var page = await _notifications.ListAsync(userId, request.IsRead, request.Page, request.PageSize, cancellationToken);
            return Result<PagedList<NotificationListItemDto>>.Success(page);
        }

        return Result<PagedList<NotificationListItemDto>>.Failure(authResult.Error!, 403);
    }
}