using Application.Common.Interfaces;
using Application.Common.Security;

namespace Application.Features.Notifications.Events;

public static class NotificationServiceExtensions
{
    public static async Task SendToStaffAsync(
        this INotificationService notifications,
        NotificationCreate create,
        CancellationToken cancellationToken = default)
    {
        await notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);
        await notifications.SendToRoleAsync(Roles.Admin, create, cancellationToken);
    }
}
