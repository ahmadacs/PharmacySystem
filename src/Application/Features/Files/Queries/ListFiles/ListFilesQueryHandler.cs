using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Exceptions;
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
        if (!Enum.TryParse<Domain.Entities.Files.FileEntityType>(request.EntityType, true, out var entityType))
            return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["InvalidEntityType", request.EntityType, "Medicine, Prescription"].Value, 422);

        if (entityType == FileEntityType.Medicine)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["FileViewMedicine"].Value, 403);
        }
        else
        {
            var prescription = await _prescriptions.GetByIdAsync(request.EntityId, cancellationToken);
            if (prescription is not null)
            {
                try
                {
                    await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
                }
                catch (ForbiddenResourceException ex)
                {
                    return Result<IReadOnlyList<FileAttachmentDto>>.Failure(ex.Message, 403);
                }
            }
            else
                return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["ResourceNotFound", "Prescription", request.EntityId].Value, 404);
        }

        var list = await _files.ListByEntityAsync(entityType, request.EntityId, cancellationToken);
        return Result<IReadOnlyList<FileAttachmentDto>>.Success(list.Select(x => x.ToDto()).ToList());
    }
}
