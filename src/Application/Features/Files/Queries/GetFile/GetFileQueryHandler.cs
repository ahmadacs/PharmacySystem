using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Common.Specifications;
using Domain.Entities.Files;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Queries.GetFile;

public sealed class GetFileQueryHandler : IRequestHandler<GetFileQuery, Result<(Stream Content, string ContentType, string FileName)>>
{
    private readonly IBaseRepository<FileAttachment> _files;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetFileQueryHandler(IBaseRepository<FileAttachment> files, IFileStorageService storage, ICurrentUserService currentUser, IBaseRepository<Prescription> prescriptions, IResourceAuthorizationService resourceAuth, IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
        _storage = storage;
        _currentUser = currentUser;
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
        _localizer = localizer;
    }

    public async Task<Result<(Stream Content, string ContentType, string FileName)>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var fileSpec = new Specification<FileAttachment, FileAttachment>(f => f).Tracked();
        fileSpec.Where(f => f.Id == request.FileId);
        var attachment = await _files.GetAsync(fileSpec, cancellationToken);
        if (attachment is null) return Result<(Stream Content, string ContentType, string FileName)>.Failure(_localizer["ResourceNotFound", "FileAttachment", request.FileId].Value, 404);

        if (attachment.EntityType == FileEntityType.Medicine)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                return Result<(Stream Content, string ContentType, string FileName)>.Failure(_localizer["FileViewMedicine"].Value, 403);
        }
        else if (attachment.EntityType == FileEntityType.Batch
            || attachment.EntityType == FileEntityType.InventoryAdjustment)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Inventory.View)
                && !_currentUser.Permissions.Contains(Permissions.Inventory.Adjust))
                return Result<(Stream Content, string ContentType, string FileName)>.Failure(_localizer["FileViewInventory"].Value, 403);
        }
        else
        {
            var prescriptionSpec = new Specification<Prescription, Prescription>(p => p).Tracked();
            prescriptionSpec.Where(p => p.Id == attachment.EntityId);
            var prescription = await _prescriptions.GetAsync(prescriptionSpec, cancellationToken);
            if (prescription is not null)
            {
                await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
            }
        }

        var (content, contentType) = await _storage.OpenReadAsync(attachment.BlobPath, cancellationToken);
        return Result<(Stream Content, string ContentType, string FileName)>.Success((content, contentType, attachment.FileName));
    }
}
