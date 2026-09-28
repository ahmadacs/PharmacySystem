using Application.Common.Models;
using Application.Features.Notifications.Commands;
using Application.Features.Notifications.Dtos;
using Application.Features.Notifications.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
[ApiVersion("1.0")]
public sealed class NotificationsController(ISender sender) : ApiControllerBase(sender)
{

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> List(
        [FromQuery] ListNotificationsQuery query,
        CancellationToken cancellationToken = default)
        => OkResponse(query, cancellationToken);

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
        => NoContent(new MarkNotificationReadCommand(id), cancellationToken);

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        => NoContent(new MarkAllNotificationsReadCommand(), cancellationToken);
}