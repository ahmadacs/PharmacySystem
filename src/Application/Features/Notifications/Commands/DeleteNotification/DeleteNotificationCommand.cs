using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Resources;
using Domain.Entities.Notifications;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Commands;

public sealed record DeleteNotificationCommand(Guid NotificationId) : IRequest<Result>;

public sealed class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly IRepositoryWithHardDelete<Notification> _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteNotificationCommandHandler(
        IRepositoryWithHardDelete<Notification> notifications,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IStringLocalizer<SharedResource> localizer)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var notification = await _notifications.GetByIdAsync(request.NotificationId, tracked: true, cancellationToken: cancellationToken);
        if (notification is null)
            return Result.Failure(_localizer["ResourceNotFound", "Notification", request.NotificationId].Value, 404);

        if (notification.UserId != userId)
            return Result.Failure(_localizer["OwnNotifications"].Value, 403);

        _notifications.HardDelete(notification);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
