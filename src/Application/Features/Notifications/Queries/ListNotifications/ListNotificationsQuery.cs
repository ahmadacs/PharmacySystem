using System.ComponentModel.DataAnnotations;
using Application.Common.Models;
using Application.Features.Notifications.Dtos;
using MediatR;

namespace Application.Features.Notifications.Queries;

public sealed record ListNotificationsQuery(
    bool? IsRead = null,
    [param: Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    int Page = 1,
    [param: Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200.")]
    int PageSize = 10) : IRequest<PagedResult<NotificationListItemDto>>;