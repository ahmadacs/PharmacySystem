using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Security;
using Application.Common.Specifications;
using Domain.Entities.Notifications;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Notifications;

/// <summary>
/// SignalR hub for in-app notifications. Clients join the "user:{id}" group on
/// connect so the NotificationService can push per-user messages. JWT is sent as
/// the "access_token" query string (WebSockets cannot send Authorization headers);
/// Program.cs forwards it to the JWT bearer middleware for /hubs paths only.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    private readonly Application.Common.Interfaces.IBaseRepository<Notification> _notifications;

    public NotificationsHub(Application.Common.Interfaces.IBaseRepository<Notification> notifications)
    {
        _notifications = notifications;
    }
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            foreach (var role in Context.User?.FindAll(JwtClaimTypes.Role).Select(c => c.Value).Distinct() ?? Enumerable.Empty<string>())
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

            if (Guid.TryParse(userId, out var uid))
            {
                var unreadSpec = new Specification<Notification, Notification>(n => n);
                unreadSpec.Where(n => n.UserId == uid && !n.IsRead);
                var count = await _notifications.CountAsync(unreadSpec);
                await Clients.Caller.SendAsync("unreadCount", count);
            }
        }

        await base.OnConnectedAsync();
    }
}