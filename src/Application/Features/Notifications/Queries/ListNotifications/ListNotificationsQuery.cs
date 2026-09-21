using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using MediatR;

namespace Application.Features.Notifications.Queries;

public sealed record ListNotificationsQuery : PagedQuery, IRequest<Result<PagedList<NotificationListItemDto>>>
{
    public bool? IsRead { get; init; }
}
