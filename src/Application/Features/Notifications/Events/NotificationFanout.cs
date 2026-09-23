using Application.Common.Interfaces;
using Application.Common.Security;

namespace Application.Features.Notifications.Events;

/// <summary>Shared fan-out for staff alerts: Pharmacists and Admins.</summary>
public static class NotificationFanout
{
    public static async Task SendToStaffAsync(
        INotificationService notifications,
        NotificationCreate create,
        CancellationToken cancellationToken)
    {
        await notifications.SendToRoleAsync(Roles.Pharmacist, create, cancellationToken);
        await notifications.SendToRoleAsync(Roles.Admin, create, cancellationToken);
    }
}
