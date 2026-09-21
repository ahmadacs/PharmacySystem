using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Microsoft.Extensions.Localization;

namespace Application.Common;

/// <summary>
/// Guard clause for "any authenticated user" checks. Returns null on success
/// (with the user id) or the ready-to-return failure, so handlers stay flat
/// instead of nesting their whole body inside <c>if (IsSuccess)</c>.
/// </summary>
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
