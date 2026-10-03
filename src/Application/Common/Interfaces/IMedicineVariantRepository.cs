using Domain.Entities.Medicines;

namespace Application.Common.Interfaces;

public interface IMedicineVariantRepository : IRepository<MedicineVariant>
{
    Task<MedicineVariant?> GetForAddBatchAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<MedicineVariant?> GetWithStockGraphAsync(Guid variantId, CancellationToken cancellationToken = default);
}
