namespace Application.Common.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>?> GetRolePermissionsAsync(string role, CancellationToken cancellationToken = default);

    Task<(bool RoleFound, IReadOnlyList<string> InvalidPermissions)> SetRolePermissionsAsync(
        string role,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);
}
