using System;

namespace Application.Features.Patients.Dtos;

public sealed record PatientCheckDto(bool Exists, string? FirstName, string? LastName, DateOnly? DateOfBirth);
