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

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var rows = await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, string>(rows.Count);
        foreach (var row in rows)
        {
            var fullName = $"{row.FirstName} {row.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                result[row.Id] = fullName;
        }

        return result;
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
        {
            await _userManager.ResetAccessFailedCountAsync(user);
            return PasswordCheckResult.Success;
        }

        await _userManager.AccessFailedAsync(user);

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

    public async Task<PagedList<UserAccount>> ListAsync(
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
            .Where(x => x.Name is not null)
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name!).ToList());

        var permissionsByRole = await GetPermissionsByRoleAsync(
            rolesByUser.Values.SelectMany(r => r).Distinct(), cancellationToken);

        var accounts = users
            .Select(u =>
            {
                var roles = rolesByUser.TryGetValue(u.Id, out var found) ? found : [];
                var permissions = roles
                    .SelectMany(r => permissionsByRole.TryGetValue(r, out var p) ? p : [])
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();
                return new UserAccount(u.Id, u.Email ?? u.UserName ?? string.Empty, u.FullName, u.IsActive, roles, permissions);
            })
            .ToList();

        return accounts.ToPagedList(page, pageSize, totalCount);
    }

    private async Task<UserAccount> ToAccountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = await GetPermissionsForRolesAsync(roles, cancellationToken);

        return new UserAccount(user.Id, user.Email ?? user.UserName ?? string.Empty, user.FullName, user.IsActive, roles, permissions);
    }

    private async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken)
    {
        var byRole = await GetPermissionsByRoleAsync(roles, cancellationToken);
        return byRole.Values
            .SelectMany(p => p)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetPermissionsByRoleAsync(
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var roleNames = roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (roleNames.Count == 0)
            return new Dictionary<string, IReadOnlyList<string>>();

        var rolesMeta = await _db.Roles
            .Where(r => r.Name != null && roleNames.Contains(r.Name))
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var roleIdByName = rolesMeta
            .Where(x => x.Name is not null)
            .ToDictionary(x => x.Name!, x => x.Id);

        if (roleIdByName.Count == 0)
            return new Dictionary<string, IReadOnlyList<string>>();

        var ids = roleIdByName.Values.ToList();
        var nameById = roleIdByName.ToDictionary(kv => kv.Value, kv => kv.Key);

        var claims = await _db.RolePermissions
            .Where(rp => ids.Contains(rp.RoleId))
            .Select(rp => new { rp.RoleId, Name = rp.Permission.Name })
            .ToListAsync(cancellationToken);

        var result = roleNames.ToDictionary(
            name => name,
            _ => (IReadOnlyList<string>)[]);

        foreach (var group in claims
            .Where(c => c.Name is not null && nameById.ContainsKey(c.RoleId))
            .GroupBy(c => nameById[c.RoleId]))
        {
            result[group.Key] = group
                .Select(c => c.Name)
                .Distinct()
                .OrderBy(p => p)
                .ToList();
        }

        return result;
    }

    private static OperationResult ToOperationResult(IdentityResult result)
        => result.Succeeded
            ? new OperationResult(true, [])
            : new OperationResult(false, result.Errors.Select(e => e.Description).ToList());
}