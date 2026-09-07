using Application.Common.Interfaces;

namespace Application.Features.Auth.Dtos;

public static class AuthMapping
{
    public static CurrentUserDto ToDto(this UserAccount account)
        => new(
            account.Id,
            account.Email,
            account.FullName,
            account.Roles.FirstOrDefault(),
            account.Permissions);

    public static AuthResponse ToResponse(AuthTokens tokens, UserAccount account)
        => new(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAtUtc,
            account.ToDto());
}