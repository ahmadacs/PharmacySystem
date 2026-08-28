using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Files;
using Domain.Exceptions;
using Application.Features.Files.Dtos;
using MediatR;

namespace Application.Features.Files.Queries.ListFiles;

public sealed class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, IReadOnlyList<FileAttachmentDto>>
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

    public async Task<IReadOnlyList<FileAttachmentDto>> Handle(ListFilesQuery request, CancellationToken cancellationToken)
    {
        if (request.EntityId == Guid.Empty) throw new FileValidationException("EntityId is required.");
        if (!Enum.TryParse<Domain.Entities.Files.FileEntityType>(request.EntityType, true, out var entityType))
            throw new FileValidationException($"Invalid entity type '{request.EntityType}'. Use Medicine or Prescription.");

        if (entityType == FileEntityType.Medicine)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                throw new ForbiddenResourceException("Missing permission to view medicine files.");
        }
        else
        {
            var prescription = await _prescriptions.GetByIdAsync(request.EntityId, cancellationToken);
            if (prescription is not null)
                await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
            else
                throw new EntityNotFoundException(typeof(Domain.Entities.Prescriptions.Prescription), request.EntityId);
        }

        var list = await _files.ListByEntityAsync(entityType, request.EntityId, cancellationToken);
        return list.Select(x => x.ToDto()).ToList();
    }
}
