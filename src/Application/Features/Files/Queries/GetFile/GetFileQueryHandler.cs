using Application.Common.Interfaces;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Files.Queries.GetFile;

public sealed class GetFileQueryHandler : IRequestHandler<GetFileQuery, (Stream Content, string ContentType, string FileName)>
{
    private readonly IFileAttachmentRepository _files;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;

    public GetFileQueryHandler(IFileAttachmentRepository files, IFileStorageService storage, ICurrentUserService currentUser, IPrescriptionRepository prescriptions, IResourceAuthorizationService resourceAuth)
    {
        _files = files;
        _storage = storage;
        _currentUser = currentUser;
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
    }

    public async Task<(Stream Content, string ContentType, string FileName)> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _files.GetByIdAsync(request.FileId, cancellationToken);
        if (attachment is null) throw new EntityNotFoundException(typeof(Domain.Entities.Files.FileAttachment), request.FileId);

        if (attachment.EntityType == Domain.Entities.Files.FileEntityType.Medicine)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                throw new ForbiddenResourceException("Missing permission to view medicine files.");
        }
        else
        {
            var prescription = await _prescriptions.GetByIdAsync(attachment.EntityId, cancellationToken);
            if (prescription is not null)
                await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
        }

        var (content, contentType) = await _storage.OpenReadAsync(attachment.BlobPath, cancellationToken);
        return (content, contentType, attachment.FileName);
    }
}
