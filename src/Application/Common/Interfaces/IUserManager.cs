using Application.Common.Models;

namespace Application.Common.Interfaces;

public enum PasswordCheckResult
{
    NotAllowed,
    LockedOut,
    Failed,
    Success
}

public sealed record UserAccount(
    Guid Id,
    string Email,
    string? FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record OperationResult(bool Succeeded, IReadOnlyList<string> Errors);

public sealed record CreateUserResult(Guid? UserId, IReadOnlyList<string> Errors)
{
    public bool Succeeded => UserId is not null;
}

/// <summary>
/// Thin adapter over ASP.NET Core Identity user/role stores used by Application
/// auth and users handlers. Keeps Identity types out of the Application layer.
/// </summary>
public interface IUserManager
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PasswordCheckResult> CheckPasswordAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Creates the user and assigns roles. Returns the new user id or a list of errors on failure.</summary>
    Task<CreateUserResult> TryCreateUserAsync(string email, string firstName, string lastName, string password, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Returns a password-reset token, or null if the email is unknown.</summary>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    Task<OperationResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<OperationResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Returns a paged, filtered and sorted page of user accounts.</summary>
    Task<PagedList<UserAccount>> ListAsync(
        string? search,
        string? role,
        bool? isActive,
        string? sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}