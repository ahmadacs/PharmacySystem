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
        => await Db.Set<Patient>()
            .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber.Trim().Replace(" ", "").Replace("-", ""), cancellationToken);
}