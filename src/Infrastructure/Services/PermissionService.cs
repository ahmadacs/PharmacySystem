using Application.Common.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _db;

    public PermissionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return [];

        return await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCatalogAsync(CancellationToken cancellationToken = default)
        => await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>?> GetRolePermissionsAsync(string role, CancellationToken cancellationToken = default)
    {
        var roleId = await FindRoleIdAsync(role, cancellationToken);
        if (roleId is null)
            return null;

        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool RoleFound, IReadOnlyList<string> InvalidPermissions)> SetRolePermissionsAsync(
        string role,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var roleId = await FindRoleIdAsync(role, cancellationToken);
        if (roleId is null)
            return (false, []);

        var requested = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var catalog = await _db.Permissions
            .ToDictionaryAsync(p => p.Name, p => p.Id, StringComparer.Ordinal, cancellationToken);

        var invalid = requested.Where(p => !catalog.ContainsKey(p)).ToList();
        if (invalid.Count > 0)
            return (true, invalid);

        var desiredIds = requested.Select(p => catalog[p]).ToHashSet();

        var existing = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();

        foreach (var stale in existing.Where(rp => !desiredIds.Contains(rp.PermissionId)))
            _db.RolePermissions.Remove(stale);

        foreach (var permissionId in desiredIds.Where(id => !existingIds.Contains(id)))
            _db.RolePermissions.Add(new RolePermission(roleId.Value, permissionId));

        await _db.SaveChangesAsync(cancellationToken);

        return (true, []);
    }

    private async Task<Guid?> FindRoleIdAsync(string role, CancellationToken cancellationToken)
        => await _db.Roles
            .Where(r => r.Name == role.Trim())
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
