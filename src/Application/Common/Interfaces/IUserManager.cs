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

public interface IUserManager
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
    Task<PasswordCheckResult> CheckPasswordAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<CreateUserResult> TryCreateUserAsync(string email, string firstName, string lastName, string password, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    Task<OperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    Task<OperationResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<OperationResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

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