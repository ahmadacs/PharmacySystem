using Application.Features.Prescriptions.Dtos;

namespace Application.Features.Patients.Dtos;

public sealed record PatientDto(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, string PhoneNumber, int Age);

/// <summary>
/// Internal EF projection shape: raw patient columns only (Age is derived
/// in <c>PatientMapping</c> — DateOnly math is not SQL).
/// </summary>
internal sealed record PatientRow(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PhoneNumber);

public static class PatientMapping
{
    public static PatientDto ToDto(this Domain.Entities.Patients.Patient p) => new(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Age);

    internal static PatientDto ToDto(this PatientRow r) => new(r.Id, r.FirstName, r.LastName, r.DateOfBirth, r.PhoneNumber, CalculateAge(r.DateOfBirth));

    internal static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > today)
            age--;

        return age;
    }

    public static Domain.Entities.Patients.Patient ToEntity(string firstName, string lastName, DateOnly dateOfBirth, string phoneNumber)
        => new(firstName, lastName, dateOfBirth, phoneNumber);

    public static PatientCheckDto ToCheckDto(this PatientDto patient)
        => new(true, patient.Id, patient.FirstName, patient.LastName, patient.DateOfBirth, patient.PhoneNumber);

    public static PatientCheckDto ToNotFoundCheck() => new(false, null, null, null, null, null);
}
