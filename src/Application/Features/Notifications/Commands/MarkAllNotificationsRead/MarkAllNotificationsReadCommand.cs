using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Common;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Notifications.Commands;

public sealed record MarkAllNotificationsReadCommand : IRequest<Result>;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
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

    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            await _notifications.MarkAllReadAsync(userId, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        return Result.Failure(authResult.Error!, authResult.StatusCode);
    }
}