using Application.Common.Interfaces;
using Domain.Entities.Patients;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext db) : base(db)
    {
    }

    public async Task<Patient?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        // Normalized on the client side (custom methods are not translatable
        // to SQL); stored numbers are canonical (+966...), so plain equality.
        var normalized = Domain.Common.PhoneNumbers.NormalizeSaudiPhone(phoneNumber);
        return await Db.Set<Patient>()
            .FirstOrDefaultAsync(p => p.PhoneNumber == normalized, cancellationToken);
    }
}