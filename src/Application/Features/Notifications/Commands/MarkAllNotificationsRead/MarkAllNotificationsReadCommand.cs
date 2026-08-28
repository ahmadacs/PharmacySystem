using Application.Common.Interfaces;
using Application.Features.Prescriptions.Common;
using MediatR;

namespace Application.Features.Notifications.Commands;

public sealed record MarkAllNotificationsReadCommand : IRequest;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);

        await _notifications.MarkAllReadAsync(userId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}