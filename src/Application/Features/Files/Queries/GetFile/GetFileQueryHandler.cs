using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Prescriptions.Common;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Files.Queries.GetFile;

public sealed class GetFileQueryHandler : IRequestHandler<GetFileQuery, Result<(Stream Content, string ContentType, string FileName)>>
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

    public async Task<Result<(Stream Content, string ContentType, string FileName)>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _files.GetByIdAsync(request.FileId, cancellationToken);
        if (attachment is null) return Result<(Stream Content, string ContentType, string FileName)>.Failure($"Resource 'FileAttachment' with id '{request.FileId}' was not found.", 404);

        if (attachment.EntityType == Domain.Entities.Files.FileEntityType.Medicine)
        {
            if (!_currentUser.Permissions.Contains(Permissions.Medicines.View))
                return Result<(Stream Content, string ContentType, string FileName)>.Failure("Missing permission to view medicine files.", 403);
        }
        else
        {
            var prescription = await _prescriptions.GetByIdAsync(attachment.EntityId, cancellationToken);
            if (prescription is not null)
            {
                try
                {
                    await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
                }
                catch (ForbiddenResourceException ex)
                {
                    return Result<(Stream Content, string ContentType, string FileName)>.Failure(ex.Message, 403);
                }
            }
        }

        var (content, contentType) = await _storage.OpenReadAsync(attachment.BlobPath, cancellationToken);
        return Result<(Stream Content, string ContentType, string FileName)>.Success((content, contentType, attachment.FileName));
    }
}
