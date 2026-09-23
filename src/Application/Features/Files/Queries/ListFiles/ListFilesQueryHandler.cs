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
using Application.Features.Files.Dtos;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Queries.ListFiles;

public sealed class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, Result<IReadOnlyList<FileAttachmentDto>>>
{
    private readonly IBaseRepository<FileAttachment> _files;
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IBaseRepository<InventoryAdjustment> _adjustments;
    private readonly ICurrentUserService _currentUser;
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListFilesQueryHandler(IBaseRepository<FileAttachment> files, IBaseRepository<Medicine> medicines, IBaseRepository<MedicineBatch> batches, IBaseRepository<InventoryAdjustment> adjustments, ICurrentUserService currentUser, IBaseRepository<Prescription> prescriptions, IResourceAuthorizationService resourceAuth, IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
        _medicines = medicines;
        _batches = batches;
        _adjustments = adjustments;
        _currentUser = currentUser;
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
        _localizer = localizer;
    }

    public async Task<Result<IReadOnlyList<FileAttachmentDto>>> Handle(ListFilesQuery request, CancellationToken cancellationToken)
    {
        if (request.EntityId == Guid.Empty) return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["EntityIdRequired"].Value, 422);
        if (!Enum.TryParse<FileEntityType>(request.EntityType, true, out var entityType))
            return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["InvalidEntityType", request.EntityType, "Medicine, Prescription, Batch, InventoryAdjustment"].Value, 422);

        switch (entityType)
        {
            case FileEntityType.Medicine:
                if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["FileViewMedicine"].Value, 403);
                if (!await ExistsAsync(_medicines, request.EntityId, cancellationToken))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "Medicine", request.EntityId].Value, 404);
                break;
            case FileEntityType.Batch:
                if (!_currentUser.Permissions.Contains(Permissions.Inventory.View)
                    && !_currentUser.Permissions.Contains(Permissions.Inventory.Adjust))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["FileViewBatch"].Value, 403);
                if (!await ExistsAsync(_batches, request.EntityId, cancellationToken))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "MedicineBatch", request.EntityId].Value, 404);
                break;
            case FileEntityType.InventoryAdjustment:
                if (!_currentUser.Permissions.Contains(Permissions.Inventory.View)
                    && !_currentUser.Permissions.Contains(Permissions.Inventory.Adjust))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["FileViewInventory"].Value, 403);
                if (!await ExistsAsync(_adjustments, request.EntityId, cancellationToken))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "InventoryAdjustment", request.EntityId].Value, 404);
                break;
            default:
            {
                var prescriptionSpec = new Specification<Prescription, Prescription>(p => p).Tracked();
                prescriptionSpec.Where(p => p.Id == request.EntityId);
                var prescription = await _prescriptions.GetAsync(prescriptionSpec, cancellationToken);
                if (prescription is not null)
                {
                    await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
                }
                else
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "Prescription", request.EntityId].Value, 404);
                break;
            }
        }

        var listSpec = new Specification<FileAttachment, FileAttachment>(f => f);
        listSpec.Where(f => f.EntityType == entityType && f.EntityId == request.EntityId);
        listSpec.Order(q => q.OrderByDescending(f => f.CreatedAt));
        var list = await _files.ListAsync(listSpec, cancellationToken);
        return Result<IReadOnlyList<FileAttachmentDto>>.Success(list.Select(x => x.ToDto()).ToList());
    }

    private static async Task<bool> ExistsAsync<TEntity>(
        IBaseRepository<TEntity> repo, Guid id, CancellationToken cancellationToken)
        where TEntity : Domain.Common.BaseEntity
    {
        var spec = new Specification<TEntity, TEntity>(e => e);
        spec.Where(e => e.Id == id);
        return await repo.GetAsync(spec, cancellationToken) is not null;
    }
}
