using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    private readonly Application.Common.Interfaces.INotificationRepository _notifications;

    public NotificationsHub(Application.Common.Interfaces.INotificationRepository notifications)
    {
        _notifications = notifications;
    }
    private const string RoleClaimType = "role";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            foreach (var role in Context.User?.FindAll(RoleClaimType).Select(c => c.Value).Distinct() ?? Enumerable.Empty<string>())
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

            if (Guid.TryParse(userId, out var uid))
            {
                var count = await _notifications.CountUnreadAsync(uid);
                await Clients.Caller.SendAsync("unreadCount", count);
            }
        }

        await base.OnConnectedAsync();
    }
}