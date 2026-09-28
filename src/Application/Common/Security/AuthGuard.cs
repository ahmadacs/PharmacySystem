using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Microsoft.Extensions.Localization;

namespace Application.Common.Security;

internal static class AuthGuard
{
    public static Result? RequireUserId(
        ICurrentUserService currentUser,
        IStringLocalizer<SharedResource> localizer,
        out Guid userId)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            userId = Guid.Empty;
            return Result.Failure(localizer["Forbidden"].Value, 403);
        }

        userId = currentUser.UserId.Value;
        return null;
    }

    public static Result<T>? RequireUserId<T>(
        ICurrentUserService currentUser,
        IStringLocalizer<SharedResource> localizer,
        out Guid userId)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            userId = Guid.Empty;
            return Result<T>.Failure(localizer["Forbidden"].Value, 403);
        }

        userId = currentUser.UserId.Value;
        return null;
    }
}
