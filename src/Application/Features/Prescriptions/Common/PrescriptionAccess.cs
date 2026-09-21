using Application.Common.Interfaces;
using Application.Common.Security;

namespace Application.Features.Prescriptions.Common;

internal static class PrescriptionAccess
{
    public static bool CanViewAll(ICurrentUserService currentUser)
        => currentUser.Permissions.Contains(Permissions.Prescriptions.View);

    public static bool CanManageOwn(ICurrentUserService currentUser)
        => currentUser.Permissions.Contains(Permissions.Prescriptions.ManageOwn);
}