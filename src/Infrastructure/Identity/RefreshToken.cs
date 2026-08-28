using Domain.Common;

namespace Infrastructure.Identity;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public ApplicationUser? User { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsExpired(DateTime asOfUtc) => asOfUtc >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive(DateTime asOfUtc) => !IsRevoked && !IsExpired(asOfUtc);

    public void Revoke(string? replacedByTokenHash = null, DateTime? revokedAtUtc = null)
    {
        RevokedAtUtc = revokedAtUtc ?? DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}