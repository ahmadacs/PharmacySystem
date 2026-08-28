using Application.Common.Interfaces;
using Application.Common.Models;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class UserManagerService : IUserManager
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public UserManagerService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<UserAccount?> FindAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToAccountAsync(user, cancellationToken);
    }

    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : await ToAccountAsync(user, cancellationToken);
    }

    public async Task<PasswordCheckResult> CheckPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return PasswordCheckResult.Failed;

        if (!user.IsActive)
            return PasswordCheckResult.NotAllowed;

        if (await _userManager.IsLockedOutAsync(user))
            return PasswordCheckResult.LockedOut;

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (valid)
            return PasswordCheckResult.Success;

        // CheckPasswordAsync increments the failed-access counter; re-check lockout.
        return await _userManager.IsLockedOutAsync(user)
            ? PasswordCheckResult.LockedOut
            : PasswordCheckResult.Failed;
    }

    public async Task<CreateUserResult> TryCreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
            return new CreateUserResult(null, ["An account with this email already exists."]);

        var existingRoles = (await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var role in roles)
        {
            if (!existingRoles.Contains(role))
            {
                var createRole = await _roleManager.CreateAsync(new ApplicationRole(role));
                if (!createRole.Succeeded)
                    return new CreateUserResult(
                        null,
                        createRole.Errors.Select(e => e.Description).ToList());
            }
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            EmailConfirmed = true,
            IsActive = true
        };

        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded)
            return new CreateUserResult(null, create.Errors.Select(e => e.Description).ToList());

        var addRoles = await _userManager.AddToRolesAsync(user, roles);
        if (!addRoles.Succeeded)
            return new CreateUserResult(null, addRoles.Errors.Select(e => e.Description).ToList());

        return new CreateUserResult(user.Id, []);
    }

    public async Task<OperationResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new OperationResult(false, ["User not found."]);

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return ToOperationResult(result);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<OperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new OperationResult(false, ["User not found."]);

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return ToOperationResult(result);
    }

    public async Task<OperationResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new OperationResult(false, ["User not found."]);

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        return ToOperationResult(result);
    }

    public async Task<PagedResult<UserAccount>> ListAsync(
        string? search,
        string? role,
        bool? isActive,
        string? sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> query = _db.Users;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            query = query.Where(u => _db.UserRoles
                .Where(ur => ur.UserId == u.Id)
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                .Any(n => n == roleName));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? sortBy?.ToLowerInvariant() switch
            {
                "fullname" => query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName),
                _ => query.OrderByDescending(u => u.Email)
            }
            : sortBy?.ToLowerInvariant() switch
            {
                "fullname" => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
                _ => query.OrderBy(u => u.Email)
            };

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        var rolesByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).ToList());

        var accounts = users
            .Select(u =>
            {
                var roles = rolesByUser.TryGetValue(u.Id, out var found) ? found : [];
                var permissions = RolePermissions.GetPermissions(roles);
                return new UserAccount(u.Id, u.Email ?? u.UserName ?? string.Empty, u.FullName, u.IsActive, roles, permissions);
            })
            .ToList();

        return new PagedResult<UserAccount>
        {
            Items = accounts,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<UserAccount> ToAccountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = RolePermissions.GetPermissions(roles);

        return new UserAccount(user.Id, user.Email ?? user.UserName ?? string.Empty, user.FullName, user.IsActive, roles, permissions);
    }

    private static OperationResult ToOperationResult(IdentityResult result)
        => result.Succeeded
            ? new OperationResult(true, [])
            : new OperationResult(false, result.Errors.Select(e => e.Description).ToList());
}