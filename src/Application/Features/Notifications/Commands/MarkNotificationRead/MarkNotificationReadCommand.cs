using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Notifications;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notifications,
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
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification is null)
                return Result.Failure(_localizer["ResourceNotFound", "Notification", request.NotificationId].Value, 404);

            if (notification.UserId != userId)
                return Result.Failure(_localizer["OwnNotifications"].Value, 403);

            try
            {
                notification.MarkRead(DateTime.UtcNow);
            }
            catch (DomainException ex)
            {
                return Result.Failure(ex.Message, 422);
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        return Result.Failure(authResult.Error!, authResult.StatusCode);
    }
}