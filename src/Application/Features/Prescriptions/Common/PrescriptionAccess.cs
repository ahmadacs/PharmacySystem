using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Exceptions;

namespace Application.Features.Prescriptions.Common;

internal static class PrescriptionAccess
{
    public static bool CanViewAll(ICurrentUserService currentUser)
        => currentUser.Permissions.Contains(Permissions.Prescriptions.View);

    public static bool CanManageOwn(ICurrentUserService currentUser)
        => currentUser.Permissions.Contains(Permissions.Prescriptions.ManageOwn);

    public static Guid RequireAuthenticatedUserId(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenResourceException();
        return currentUser.UserId.Value;
    }
}