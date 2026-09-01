using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Notifications;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Notifications.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notifications,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification is null)
                return Result.Failure($"Resource 'Notification' with id '{request.NotificationId}' was not found.", 404);

            if (notification.UserId != userId)
                return Result.Failure("You can only manage your own notifications.", 403);

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