namespace Application.Common.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Pharmacist = "Pharmacist";
    public const string Doctor = "Doctor";

    public static IReadOnlyList<string> All { get; } = [Admin, Pharmacist, Doctor];
}