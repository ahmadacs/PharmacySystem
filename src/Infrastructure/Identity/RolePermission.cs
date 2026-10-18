namespace Infrastructure.Identity;

public class RolePermission
{
    public Guid RoleId { get; private set; }
    public ApplicationRole? Role { get; private set; }
    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
