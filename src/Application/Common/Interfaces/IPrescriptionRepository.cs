using Domain.Entities.Prescriptions;

namespace Application.Common.Interfaces;

public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    Task<Prescription?> GetForDispensingAsync(string shortCode, string normalizedPhone, CancellationToken cancellationToken = default);

    Task<Prescription?> GetForRefillAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
}
