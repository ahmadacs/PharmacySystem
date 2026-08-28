namespace Application.Common.Interfaces;

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc, Guid UserId);

/// <summary>
/// Issues short-lived JWT access tokens and manages long-lived refresh tokens
/// (created hashed, rotated on every use and revocable).
/// </summary>
public interface ITokenService
{
    Task<AuthTokens> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}