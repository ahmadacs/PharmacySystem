using Application.Common.Interfaces;
using Domain.Entities.Files;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class FileAttachmentRepository : IFileAttachmentRepository
{
    private readonly ApplicationDbContext _db;
    public FileAttachmentRepository(ApplicationDbContext db) => _db = db;

    public Task<bool> MedicineExistsAsync(Guid medicineId, CancellationToken ct) => _db.Medicines.AnyAsync(x => x.Id == medicineId, ct);
    public Task<bool> PrescriptionExistsAsync(Guid prescriptionId, CancellationToken ct) => _db.Prescriptions.AnyAsync(x => x.Id == prescriptionId, ct);
    public Task<bool> BatchExistsAsync(Guid batchId, CancellationToken ct) => _db.MedicineBatches.AnyAsync(x => x.Id == batchId, ct);
    public Task<bool> InventoryAdjustmentExistsAsync(Guid adjustmentId, CancellationToken ct) => _db.InventoryAdjustments.AnyAsync(x => x.Id == adjustmentId, ct);
    public Task<FileAttachment?> GetByIdAsync(Guid id, CancellationToken ct) => _db.FileAttachments.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<FileAttachment>> ListByEntityAsync(FileEntityType entityType, Guid entityId, CancellationToken ct)
        => await _db.FileAttachments.Where(x => x.EntityType == entityType && x.EntityId == entityId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    public void Add(FileAttachment attachment) => _db.FileAttachments.Add(attachment);
}
