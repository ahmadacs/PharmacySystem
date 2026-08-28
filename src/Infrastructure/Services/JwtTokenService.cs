using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Exceptions;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public sealed class JwtTokenService : ITokenService
{
    private const string PermissionClaimType = "permission";

    private readonly JwtOptions _options;
    private readonly IUserManager _users;
    private readonly ApplicationDbContext _db;

    public JwtTokenService(
        IOptions<JwtOptions> options,
        IUserManager users,
        ApplicationDbContext db)
    {
        _options = options.Value;
        _users = users;
        _db = db;
    }

    public async Task<AuthTokens> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (accessToken, expiresAtUtc) = await GenerateAccessTokenAsync(userId, cancellationToken);
        var (refreshToken, refreshHash) = GenerateRefreshToken();

        _db.Set<RefreshToken>().Add(new RefreshToken(
            userId,
            refreshHash,
            DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays)));

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(accessToken, refreshToken, expiresAtUtc, userId);
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsActive(DateTime.UtcNow))
            throw new InvalidRefreshTokenException();

        var user = await _users.FindAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new InvalidRefreshTokenException();

        var (accessToken, expiresAtUtc) = await GenerateAccessTokenAsync(user, cancellationToken);
        var (newRefreshToken, newHash) = GenerateRefreshToken();

        stored.Revoke(newHash);

        _db.Set<RefreshToken>().Add(new RefreshToken(
            stored.UserId,
            newHash,
            DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays)));

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(accessToken, newRefreshToken, expiresAtUtc, stored.UserId);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is not null && !stored.IsRevoked)
        {
            stored.Revoke(null);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(string Token, DateTime ExpiresAtUtc)> GenerateAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await _users.FindAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        return await GenerateAccessTokenAsync(account, cancellationToken);
    }

    private Task<(string Token, DateTime ExpiresAtUtc)> GenerateAccessTokenAsync(UserAccount account, CancellationToken cancellationToken)
    {

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Name, account.FullName ?? account.Email)
        };

        foreach (var role in account.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in account.Permissions)
            claims.Add(new Claim(PermissionClaimType, permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return Task.FromResult((new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc));
    }

    private static (string Token, string Hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return (token, HashToken(token));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}