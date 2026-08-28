using Application.Common.Interfaces;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Notifications;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Notifications.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
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

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);

        var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Notification), request.NotificationId);

        if (notification.UserId != userId)
            throw new ForbiddenResourceException("You can only manage your own notifications.");

        notification.MarkRead(DateTime.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}