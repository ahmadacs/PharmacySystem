using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Commands;

public sealed record MarkAllNotificationsReadCommand : IRequest<Result>;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IAsyncQueryExecutor executor,
        IStringLocalizer<SharedResource> localizer)
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _uow = uow;
        _executor = executor;
        _localizer = localizer;
    }

        public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        var unread = await _executor.ToListAsync(
            _notifications.Query().Where(n => n.UserId == userId && !n.IsRead),
            cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
            notification.MarkRead(now);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}