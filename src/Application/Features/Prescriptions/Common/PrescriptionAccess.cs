using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;

namespace Application.Features.Prescriptions.Common;

internal static class PrescriptionAccess
{
    public static bool CanViewAll(ICurrentUserService currentUser)
        => currentUser.Permissions.Contains(Permissions.Prescriptions.View);

    public static bool CanManageOwn(ICurrentUserService currentUser)
        => currentUser.Permissions.Contains(Permissions.Prescriptions.ManageOwn);

    public static Result<Guid> RequireAuthenticatedUserId(
        ICurrentUserService currentUser,
        Microsoft.Extensions.Localization.IStringLocalizer<Application.Resources.SharedResource> localizer)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<Guid>.Failure(localizer["Forbidden"].Value, 403);
        return Result<Guid>.Success(currentUser.UserId.Value);
    }
}