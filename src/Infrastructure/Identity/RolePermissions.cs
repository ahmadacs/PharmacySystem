using Application.Common.Security;

namespace Infrastructure.Identity;

/// <summary>
/// Maps each role to the permission names it grants. Seeding and the JWT claim
/// builder share this single mapping so permissions never drift between layers.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [Roles.Admin] = Permissions.All,

            [Roles.Pharmacist] = new[]
            {
                Permissions.Medicines.View,
                Permissions.Inventory.View,
                Permissions.Inventory.Adjust,
                Permissions.Prescriptions.View,
                Permissions.Dispensing.View,
                Permissions.Dispensing.Create
            },

            [Roles.Doctor] = new[]
            {
                Permissions.Medicines.View,
                Permissions.Prescriptions.Create,
                Permissions.Prescriptions.ManageOwn
            }
        };

    /// <summary>Returns the permission names granted by a role (empty for unknown roles).</summary>
    public static IReadOnlyList<string> GetPermissions(string role)
        => Map.TryGetValue(role, out var permissions) ? permissions : [];

    public static IReadOnlyList<string> GetPermissions(IEnumerable<string> roles)
        => roles.SelectMany(GetPermissions).Distinct().ToList();
}