using System;

namespace Application.Features.Patients.Dtos;

public sealed record PatientCheckDto(
    bool Exists,
    Guid? Id,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber);
