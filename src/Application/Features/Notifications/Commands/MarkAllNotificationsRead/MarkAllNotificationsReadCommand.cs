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
    private readonly IAsyncQueryExecutor _executor;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IAsyncQueryExecutor executor)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _uow = uow;
        _executor = executor;
    }

        public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var authResult = PrescriptionAccess.RequireAuthenticatedUserId(_currentUser);
        if (authResult.IsSuccess)
        {
            var userId = authResult.Value;

            var unread = await _executor.ToListAsync(
                _notifications.Query().Where(n => n.UserId == userId && !n.IsRead),
                cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var notification in unread)
                notification.MarkRead(now);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        return Result.Failure(authResult.Error!, authResult.StatusCode);
    }
}