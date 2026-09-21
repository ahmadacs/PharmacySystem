using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Enums;
using Application.Features.Files.Dtos;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Queries.ListFiles;

public sealed class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, Result<IReadOnlyList<FileAttachmentDto>>>
{
    private readonly IFileAttachmentRepository _files;
    private readonly ICurrentUserService _currentUser;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListFilesQueryHandler(IFileAttachmentRepository files, ICurrentUserService currentUser, IPrescriptionRepository prescriptions, IResourceAuthorizationService resourceAuth, IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
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
                if (!await _files.MedicineExistsAsync(request.EntityId, cancellationToken))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "Medicine", request.EntityId].Value, 404);
                break;
            case FileEntityType.Batch:
                if (!_currentUser.Permissions.Contains(Permissions.Inventory.View)
                    && !_currentUser.Permissions.Contains(Permissions.Inventory.Adjust))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["FileViewBatch"].Value, 403);
                if (!await _files.BatchExistsAsync(request.EntityId, cancellationToken))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "MedicineBatch", request.EntityId].Value, 404);
                break;
            case FileEntityType.InventoryAdjustment:
                if (!_currentUser.Permissions.Contains(Permissions.Inventory.View)
                    && !_currentUser.Permissions.Contains(Permissions.Inventory.Adjust))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["FileViewInventory"].Value, 403);
                if (!await _files.InventoryAdjustmentExistsAsync(request.EntityId, cancellationToken))
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "InventoryAdjustment", request.EntityId].Value, 404);
                break;
            default:
            {
                var prescription = await _prescriptions.GetByIdAsync(request.EntityId, cancellationToken);
                if (prescription is not null)
                {
                    await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
                }
                else
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "Prescription", request.EntityId].Value, 404);
                break;
            }
        }

        var list = await _files.ListByEntityAsync(entityType, request.EntityId, cancellationToken);
        return Result<IReadOnlyList<FileAttachmentDto>>.Success(list.Select(x => x.ToDto()).ToList());
    }
}
