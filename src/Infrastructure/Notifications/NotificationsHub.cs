using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Notifications;

[Authorize]
public sealed class NotificationsHub : Hub
{
    private readonly Application.Common.Interfaces.IRepository<Notification> _notifications;

    public NotificationsHub(Application.Common.Interfaces.IRepository<Notification> notifications)
    {
        _notifications = notifications;
    }
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            if (Guid.TryParse(userId, out var uid))
            {
                var count = await _notifications.CountAsync(n => n.UserId == uid && !n.IsRead);
                await Clients.Caller.SendAsync("unreadCount", count);
            }
        }

        await base.OnConnectedAsync();
    }
}