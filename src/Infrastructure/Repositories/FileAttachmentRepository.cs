using Application.Common.Interfaces;
using Domain.Entities.Files;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class FileAttachmentRepository : BaseRepository<FileAttachment>, IFileAttachmentRepository
{
    public FileAttachmentRepository(ApplicationDbContext db) : base(db)
    {
    }

    public Task<bool> MedicineExistsAsync(Guid medicineId, CancellationToken ct) => Db.Medicines.AnyAsync(x => x.Id == medicineId, ct);
    public Task<bool> BatchExistsAsync(Guid batchId, CancellationToken ct) => Db.MedicineBatches.AnyAsync(x => x.Id == batchId, ct);
    public Task<bool> InventoryAdjustmentExistsAsync(Guid adjustmentId, CancellationToken ct) => Db.InventoryAdjustments.AnyAsync(x => x.Id == adjustmentId, ct);
    public async Task<IReadOnlyList<FileAttachment>> ListByEntityAsync(FileEntityType entityType, Guid entityId, CancellationToken ct)
        => await Db.FileAttachments.Where(x => x.EntityType == entityType && x.EntityId == entityId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
}
