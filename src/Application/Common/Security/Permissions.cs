namespace Application.Common.Security;

public static class Permissions
{
    public static class Medicines
    {
        public const string View = "Permissions.Medicines.View";
        public const string Create = "Permissions.Medicines.Create";
        public const string Update = "Permissions.Medicines.Update";
        public const string Delete = "Permissions.Medicines.Delete";
    }

    public static class Inventory
    {
        public const string View = "Permissions.Inventory.View";
        public const string Adjust = "Permissions.Inventory.Adjust";
    }

    public static class Prescriptions
    {
        public const string View = "Permissions.Prescriptions.View";
        public const string Create = "Permissions.Prescriptions.Create";
        public const string ManageOwn = "Permissions.Prescriptions.ManageOwn";

        public const string ManageAll = "Permissions.Prescriptions.ManageAll";
    }

    public static class Dispensing
    {
        public const string View = "Permissions.Dispensing.View";
        public const string Create = "Permissions.Dispensing.Create";
    }

    public static class Users
    {
        public const string Manage = "Permissions.Users.Manage";
    }

    public static class AuditLog
    {
        public const string View = "Permissions.AuditLog.View";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Medicines.View, Medicines.Create, Medicines.Update, Medicines.Delete,
        Inventory.View, Inventory.Adjust,
        Prescriptions.View, Prescriptions.Create, Prescriptions.ManageOwn, Prescriptions.ManageAll,
        Dispensing.View, Dispensing.Create,
        Users.Manage,
        AuditLog.View
    ];
}