using Application.Common.Interfaces;
using Domain.Entities.Prescriptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PrescriptionRepository : BaseRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ApplicationDbContext db) : base(db)
    {
    }

    public Task<Prescription?> GetForDispensingAsync(string shortCode, string normalizedPhone, CancellationToken cancellationToken = default)
    {
        return Db.Set<Prescription>()
            .Include(p => p.Patient)
            .Include(p => p.Items).ThenInclude(i => i.MedicineVariant!).ThenInclude(v => v.Batches)
            .Include(p => p.Items).ThenInclude(i => i.MedicineVariant!).ThenInclude(v => v.Medicine)
            .AsSplitQuery()
            .Where(p => p.ShortCode == shortCode
                && p.Patient != null
                && p.Patient.PhoneNumber == normalizedPhone)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Prescription?> GetForRefillAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        return Db.Set<Prescription>()
            .Include(p => p.Items)
            .Where(p => p.Id == prescriptionId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
