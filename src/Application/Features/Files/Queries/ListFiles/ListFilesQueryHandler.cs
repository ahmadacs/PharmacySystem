using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Files;
using Domain.Exceptions;
using Application.Features.Files.Dtos;
using MediatR;

namespace Application.Features.Files.Queries.ListFiles;

public sealed class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, Result<IReadOnlyList<FileAttachmentDto>>>
{
    private readonly IFileAttachmentRepository _files;
    private readonly ICurrentUserService _currentUser;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;

    public ListFilesQueryHandler(IFileAttachmentRepository files, ICurrentUserService currentUser, IPrescriptionRepository prescriptions, IResourceAuthorizationService resourceAuth)
    {
        _files = files;
        _currentUser = currentUser;
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
    }

    public async Task<Result<IReadOnlyList<FileAttachmentDto>>> Handle(ListFilesQuery request, CancellationToken cancellationToken)
    {
        if (request.EntityId == Guid.Empty) return Result<IReadOnlyList<FileAttachmentDto>>.Failure("EntityId is required.", 422);
        if (!Enum.TryParse<Domain.Entities.Files.FileEntityType>(request.EntityType, true, out var entityType))
            return Result<IReadOnlyList<FileAttachmentDto>>.Failure($"Invalid entity type '{request.EntityType}'. Use Medicine or Prescription.", 422);

        if (entityType == FileEntityType.Medicine)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                return Result<IReadOnlyList<FileAttachmentDto>>.Failure("Missing permission to view medicine files.", 403);
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
                return Result<IReadOnlyList<FileAttachmentDto>>.Failure($"Resource 'Prescription' with id '{request.EntityId}' was not found.", 404);
        }

        var list = await _files.ListByEntityAsync(entityType, request.EntityId, cancellationToken);
        return Result<IReadOnlyList<FileAttachmentDto>>.Success(list.Select(x => x.ToDto()).ToList());
    }
}
