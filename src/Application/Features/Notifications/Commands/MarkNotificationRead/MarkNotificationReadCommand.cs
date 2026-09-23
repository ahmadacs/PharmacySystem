using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Resources;
using Domain.Entities.Notifications;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IBaseRepository<Notification> _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MarkNotificationReadCommandHandler(
        IBaseRepository<Notification> notifications,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IStringLocalizer<SharedResource> localizer)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var byIdSpec = new Specification<Notification, Notification>(n => n).Tracked();
        byIdSpec.Where(n => n.Id == request.NotificationId);
        var notification = await _notifications.GetAsync(byIdSpec, cancellationToken);
        if (notification is null)
            return Result.Failure(_localizer["ResourceNotFound", "Notification", request.NotificationId].Value, 404);

        if (notification.UserId != userId)
            return Result.Failure(_localizer["OwnNotifications"].Value, 403);

        notification.MarkRead(DateTime.UtcNow);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}