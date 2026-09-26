using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Common.Specifications;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Common;

public sealed class FileAccessChecker : IFileAccessChecker
{
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IBaseRepository<InventoryAdjustment> _adjustments;
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly ICurrentUserService _currentUser;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public FileAccessChecker(
        IBaseRepository<Medicine> medicines,
        IBaseRepository<MedicineBatch> batches,
        IBaseRepository<InventoryAdjustment> adjustments,
        IBaseRepository<Prescription> prescriptions,
        ICurrentUserService currentUser,
        IResourceAuthorizationService resourceAuth,
        IStringLocalizer<SharedResource> localizer)
    {
        _medicines = medicines;
        _batches = batches;
        _adjustments = adjustments;
        _prescriptions = prescriptions;
        _currentUser = currentUser;
        _resourceAuth = resourceAuth;
        _localizer = localizer;
    }

    public async Task<Result?> EnsureCanAttachAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken)
    {
        if (entityType == FileEntityType.Prescription)
            return await CheckPrescriptionAsync(entityId, missingAsNotFound: true, cancellationToken);

        if (entityType == FileEntityType.Medicine)
        {
            if (!HasAny(Permissions.Medicines.Create, Permissions.Medicines.Update))
                return Denied("FileUploadMedicine");
            if (!await ExistsAsync(_medicines, entityId, cancellationToken))
                return NotFound("Medicine", entityId);
            return null;
        }

        if (entityType == FileEntityType.Batch)
        {
            if (!HasAny(Permissions.Inventory.View, Permissions.Inventory.Adjust))
                return Denied("FileUploadBatch");
            if (!await ExistsAsync(_batches, entityId, cancellationToken))
                return NotFound("MedicineBatch", entityId);
            return null;
        }

        if (!HasAny(Permissions.Inventory.Adjust))
            return Denied("FileUploadInventory");
        if (!await ExistsAsync(_adjustments, entityId, cancellationToken))
            return NotFound("InventoryAdjustment", entityId);
        return null;
    }

    public async Task<Result?> EnsureCanViewAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken)
    {
        if (entityType == FileEntityType.Prescription)
            return await CheckPrescriptionAsync(entityId, missingAsNotFound: true, cancellationToken);

        if (entityType == FileEntityType.Medicine)
        {
            if (!HasAny(Permissions.Medicines.View))
                return Denied("FileViewMedicine");
            if (!await ExistsAsync(_medicines, entityId, cancellationToken))
                return NotFound("Medicine", entityId);
            return null;
        }

        if (entityType == FileEntityType.Batch)
        {
            if (!HasAny(Permissions.Inventory.View, Permissions.Inventory.Adjust))
                return Denied("FileViewBatch");
            if (!await ExistsAsync(_batches, entityId, cancellationToken))
                return NotFound("MedicineBatch", entityId);
            return null;
        }

        if (!HasAny(Permissions.Inventory.View, Permissions.Inventory.Adjust))
            return Denied("FileViewInventory");
        if (!await ExistsAsync(_adjustments, entityId, cancellationToken))
            return NotFound("InventoryAdjustment", entityId);
        return null;
    }

    public async Task<Result?> EnsureCanViewAsync(FileAttachment attachment, CancellationToken cancellationToken)
    {
        if (attachment.EntityType == FileEntityType.Medicine)
            return HasAny(Permissions.Medicines.View) ? null : Denied("FileViewMedicine");

        if (attachment.EntityType is FileEntityType.Batch or FileEntityType.InventoryAdjustment)
            return HasAny(Permissions.Inventory.View, Permissions.Inventory.Adjust) ? null : Denied("FileViewInventory");

        return await CheckPrescriptionAsync(attachment.EntityId, missingAsNotFound: false, cancellationToken);
    }

    private async Task<Result?> CheckPrescriptionAsync(Guid prescriptionId, bool missingAsNotFound, CancellationToken cancellationToken)
    {
        var spec = new Specification<Prescription, Prescription>(p => p).Tracked();
        spec.Where(p => p.Id == prescriptionId);
        var prescription = await _prescriptions.GetAsync(spec, cancellationToken);

        if (prescription is null)
            return missingAsNotFound ? NotFound("Prescription", prescriptionId) : null;

        await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
        return null;
    }

    private bool HasAny(params string[] permissions)
        => permissions.Any(p => _currentUser.Permissions.Contains(p));

    private static async Task<bool> ExistsAsync<TEntity>(IBaseRepository<TEntity> repo, Guid id, CancellationToken cancellationToken)
        where TEntity : Domain.Common.BaseEntity
    {
        var spec = new Specification<TEntity, TEntity>(e => e);
        spec.Where(e => e.Id == id);
        return await repo.GetAsync(spec, cancellationToken) is not null;
    }

    private Result Denied(string messageKey)
        => Result.Failure(_localizer[messageKey].Value, 403);

    private Result NotFound(string resource, Guid id)
        => Result.Failure(_localizer["ResourceNotFound", resource, id].Value, 404);
}
