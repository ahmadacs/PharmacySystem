using Application.Common.Interfaces;
using Domain.Entities.Medicines;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MedicineVariantRepository : BaseRepository<MedicineVariant>, IMedicineVariantRepository
{
    public MedicineVariantRepository(ApplicationDbContext db) : base(db)
    {
    }

    public Task<MedicineVariant?> GetForAddBatchAsync(Guid variantId, CancellationToken cancellationToken = default)
        => GetWithStockGraphAsync(variantId, cancellationToken);

    public Task<MedicineVariant?> GetWithStockGraphAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return Db.Set<MedicineVariant>()
            .Include(v => v.Medicine)
            .Include(v => v.Batches)
            .Where(v => v.Id == variantId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
