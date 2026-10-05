using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Common;

public sealed class FileAccessChecker : IFileAccessChecker
{
    private readonly IRepository<Medicine> _medicines;
    private readonly IRepository<MedicineBatch> _batches;
    private readonly IRepository<InventoryAdjustment> _adjustments;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly ICurrentUserService _currentUser;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public FileAccessChecker(
        IRepository<Medicine> medicines,
        IRepository<MedicineBatch> batches,
        IRepository<InventoryAdjustment> adjustments,
        IRepository<Prescription> prescriptions,
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

    public Task<Result?> EnsureCanAttachAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken)
        => EnsureAsync(entityType, entityId, forAttach: true, cancellationToken);

    public Task<Result?> EnsureCanViewAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken)
        => EnsureAsync(entityType, entityId, forAttach: false, cancellationToken);

    private async Task<Result?> EnsureAsync(FileEntityType entityType, Guid entityId, bool forAttach, CancellationToken cancellationToken)
    {
        if (entityType == FileEntityType.Prescription)
            return await CheckPrescriptionAsync(entityId, cancellationToken);

        // Single table: required permissions + parent existence per entity type.
        string[] permissions = (entityType, forAttach) switch
        {
            (FileEntityType.Medicine, true) => [Permissions.Medicines.Create, Permissions.Medicines.Update],
            (FileEntityType.Medicine, false) => [Permissions.Medicines.View],
            (FileEntityType.Batch, _) => [Permissions.Inventory.View, Permissions.Inventory.Adjust],
            (_, true) => [Permissions.Inventory.Adjust],
            _ => [Permissions.Inventory.View, Permissions.Inventory.Adjust],
        };

        string deniedKey = (entityType, forAttach) switch
        {
            (FileEntityType.Medicine, true) => "FileUploadMedicine",
            (FileEntityType.Medicine, false) => "FileViewMedicine",
            (FileEntityType.Batch, true) => "FileUploadBatch",
            (FileEntityType.Batch, false) => "FileViewBatch",
            (_, true) => "FileUploadInventory",
            _ => "FileViewInventory",
        };

        if (!HasAny(permissions))
            return Denied(deniedKey);

        var (exists, resource) = entityType switch
        {
            FileEntityType.Medicine => (await ExistsAsync(_medicines, entityId, cancellationToken), "Medicine"),
            FileEntityType.Batch => (await ExistsAsync(_batches, entityId, cancellationToken), "MedicineBatch"),
            _ => (await ExistsAsync(_adjustments, entityId, cancellationToken), "InventoryAdjustment"),
        };

        return exists ? null : NotFound(resource, entityId);
    }

    private async Task<Result?> CheckPrescriptionAsync(Guid prescriptionId, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(prescriptionId, cancellationToken: cancellationToken);

        if (prescription is null)
            return NotFound("Prescription", prescriptionId);

        await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
        return null;
    }

    private bool HasAny(params string[] permissions)
        => permissions.Any(p => _currentUser.Permissions.Contains(p));

    private static Task<bool> ExistsAsync<TEntity>(IRepository<TEntity> repo, Guid id, CancellationToken cancellationToken)
        where TEntity : Domain.Common.Entity
        => repo.ExistsAsync(e => e.Id == id, cancellationToken);

    private Result Denied(string messageKey)
        => Result.Failure(_localizer[messageKey].Value, 403);

    private Result NotFound(string resource, Guid id)
        => Result.Failure(_localizer["ResourceNotFound", resource, id].Value, 404);
}
