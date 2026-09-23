using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Resources;
using Domain.Entities.Notifications;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Notifications.Commands;

public sealed record MarkAllNotificationsReadCommand : IRequest<Result>;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly IBaseRepository<Notification> _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MarkAllNotificationsReadCommandHandler(
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

        public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var authFailure = AuthGuard.RequireUserId(_currentUser, _localizer, out var userId);
        if (authFailure is not null)
            return authFailure;

        // Tracked read: the entities are mutated below, so no AsNoTracking.
        var spec = new Specification<Notification, Notification>(n => n).Tracked();
        spec.Where(n => n.UserId == userId && !n.IsRead);

        var unread = await _notifications.ListAsync(spec, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
            notification.MarkRead(now);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
