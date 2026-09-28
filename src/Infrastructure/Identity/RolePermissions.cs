using Application.Common.Security;

namespace Infrastructure.Identity;

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

    public static IReadOnlyList<string> GetPermissions(string role)
        => Map.TryGetValue(role, out var permissions) ? permissions : [];

    public static IReadOnlyList<string> GetPermissions(IEnumerable<string> roles)
        => roles.SelectMany(GetPermissions).Distinct().ToList();
}