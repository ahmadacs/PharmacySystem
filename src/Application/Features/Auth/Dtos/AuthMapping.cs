using Application.Common.Interfaces;

namespace Application.Features.Auth.Dtos;

public static class AuthMapping
{
    public static AuthResponse ToResponse(AuthTokens tokens, UserAccount account)
        => new(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAtUtc,
            new CurrentUserDto(
                account.Id,
                account.Email,
                account.FullName,
                account.Roles.FirstOrDefault(),
                account.Permissions));
}