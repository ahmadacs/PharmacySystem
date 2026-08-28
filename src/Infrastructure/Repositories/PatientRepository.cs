using Application.Common.Interfaces;
using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Patients;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<Patient?> FindByNameAsync(string firstName, string lastName, CancellationToken cancellationToken = default)
        => await Db.Set<Patient>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.FirstName == firstName.Trim() && p.LastName == lastName.Trim(), cancellationToken);

    public async Task<Patient?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => await Db.Set<Patient>()
            .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber.Trim().Replace(" ", "").Replace("-", ""), cancellationToken);

    public async Task<Patient> GetOrCreateAsync(string firstName, string lastName, DateOnly dateOfBirth, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phoneNumber.Trim().Replace(" ", "").Replace("-", "");
        var patient = await Db.Set<Patient>().FirstOrDefaultAsync(p => p.PhoneNumber == normalizedPhone, cancellationToken);
        if (patient is not null)
        {
            // Verify identity: phone is unique, but if name/DOB mismatch, it's a different person trying to use same phone
            if (!string.Equals(patient.FirstName, firstName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(patient.LastName, lastName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                patient.DateOfBirth != dateOfBirth)
            {
                // Phone collision with different identity - treat as validation error; caller will surface 409
                throw new Domain.Exceptions.ConflictingOperationException($"Phone number {normalizedPhone} is already registered to another patient.");
            }
            return patient;
        }

        // Fallback: check by name+DOB to prevent duplicate patient with different phone
        var byNameDob = await Db.Set<Patient>().FirstOrDefaultAsync(
            p => p.FirstName == firstName.Trim() && p.LastName == lastName.Trim() && p.DateOfBirth == dateOfBirth, cancellationToken);
        if (byNameDob is not null)
        {
            // Update phone if this existing patient has different phone (should not happen after migration)
            if (byNameDob.PhoneNumber != normalizedPhone)
                byNameDob.UpdatePhone(normalizedPhone);
            return byNameDob;
        }

        var newPatient = new Patient(firstName, lastName, dateOfBirth, normalizedPhone);
        Db.Set<Patient>().Add(newPatient);
        return newPatient;
    }

    public async Task<IReadOnlyList<PrescriptionListItemDto>> GetPrescriptionsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var list = await Db.Set<Domain.Entities.Prescriptions.Prescription>()
            .Where(p => p.PatientId == patientId)
            .Include(p => p.Patient)
            .Include(p => p.Items)
            .OrderByDescending(p => p.IssuedDate)
            .ToListAsync(cancellationToken);
        return list.Select(p => p.ToListItemDto(p.DoctorId.ToString()[..8])).ToList();
    }
}