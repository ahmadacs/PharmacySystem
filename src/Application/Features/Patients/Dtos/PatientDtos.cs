using Application.Features.Prescriptions.Dtos;

namespace Application.Features.Patients.Dtos;

public sealed record PatientDto(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, string PhoneNumber, int Age);

public static class PatientMapping
{
    public static PatientDto ToDto(this Domain.Entities.Patients.Patient p) => new(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Age);

    public static Domain.Entities.Patients.Patient ToEntity(string firstName, string lastName, DateOnly dateOfBirth, string phoneNumber)
        => new(firstName, lastName, dateOfBirth, phoneNumber);

    public static PatientCheckDto ToCheckDto(this PatientDto patient)
        => new(true, patient.FirstName, patient.LastName, patient.DateOfBirth);

    public static PatientCheckDto ToNotFoundCheck() => new(false, null, null, null);
}
