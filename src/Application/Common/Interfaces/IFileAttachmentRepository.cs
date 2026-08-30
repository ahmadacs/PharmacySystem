using Domain.Entities.Files;

namespace Application.Common.Interfaces;

public interface IFileAttachmentRepository
{
    Task<bool> MedicineExistsAsync(Guid medicineId, CancellationToken cancellationToken = default);
    Task<bool> PrescriptionExistsAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<bool> BatchExistsAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<bool> InventoryAdjustmentExistsAsync(Guid adjustmentId, CancellationToken cancellationToken = default);
    Task<FileAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileAttachment>> ListByEntityAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);
    void Add(FileAttachment attachment);
}
