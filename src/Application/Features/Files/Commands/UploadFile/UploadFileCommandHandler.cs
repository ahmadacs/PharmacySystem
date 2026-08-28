using Application.Common.Interfaces;
using Application.Common.Options;
using Application.Common.Security;
using Application.Features.Files.Dtos;
using Application.Features.Prescriptions.Common;
using Domain.Entities.Files;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Files.Commands.UploadFile;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileAttachmentDto>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "application/pdf"
    };

    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        ["image/jpeg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        ["image/png"] = [new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }],
        ["application/pdf"] = [new byte[] { 0x25, 0x50, 0x44, 0x46 }]
    };

    private const long DefaultMaxSize = 5 * 1024 * 1024;

    private readonly IFileAttachmentRepository _files;
    private readonly IFileStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly FileStorageOptions _options;
    private readonly ICurrentUserService _currentUser;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;

    public UploadFileCommandHandler(IFileAttachmentRepository files, IFileStorageService storage, IUnitOfWork uow, IOptions<FileStorageOptions> options, ICurrentUserService currentUser, IPrescriptionRepository prescriptions, IResourceAuthorizationService resourceAuth)
    {
        _files = files;
        _storage = storage;
        _uow = uow;
        _options = options.Value;
        _currentUser = currentUser;
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
    }

    public async Task<FileAttachmentDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new FileValidationException($"File type '{request.ContentType}' is not allowed. Allowed: jpeg, png, pdf.");

        var maxSize = _options.MaxFileSizeBytes > 0 ? _options.MaxFileSizeBytes : DefaultMaxSize;
        if (request.SizeBytes > maxSize)
            throw new FileValidationException($"File size {request.SizeBytes} exceeds limit {maxSize} bytes (5MB).");

        if (request.SizeBytes <= 0)
            throw new FileValidationException("File is empty.");

        if (!Enum.TryParse<FileEntityType>(request.EntityType, true, out var entityType))
            throw new FileValidationException($"Invalid entity type '{request.EntityType}'. Use Medicine or Prescription.");

        // Authorization per entity type
        if (entityType == FileEntityType.Medicine)
        {
            var hasPerm = _currentUser.Permissions.Contains(Permissions.Medicines.Create) || _currentUser.Permissions.Contains(Permissions.Medicines.Update);
            if (!hasPerm) throw new ForbiddenResourceException("Missing permission to upload medicine files.");
            var exists = await _files.MedicineExistsAsync(request.EntityId, cancellationToken);
            if (!exists) throw new EntityNotFoundException(typeof(Domain.Entities.Medicines.Medicine), request.EntityId);
        }
        else
        {
            var exists = await _files.PrescriptionExistsAsync(request.EntityId, cancellationToken);
            if (!exists) throw new EntityNotFoundException(typeof(Domain.Entities.Prescriptions.Prescription), request.EntityId);
            var prescription = await _prescriptions.GetByIdAsync(request.EntityId, cancellationToken);
            if (prescription is not null)
                await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
        }

        // Validate extension matches content type
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        var allowedExt = request.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => new[] { ".jpg", ".jpeg" },
            "image/png" => new[] { ".png" },
            "application/pdf" => new[] { ".pdf" },
            _ => Array.Empty<string>()
        };
        if (!allowedExt.Contains(ext))
            throw new FileValidationException($"File extension '{ext}' does not match content type '{request.ContentType}'.");

        var header = new byte[8];
        request.Content.Position = 0;
        var read = await request.Content.ReadAsync(header, 0, 8, cancellationToken);
        request.Content.Position = 0;
        if (!HasValidSignature(request.ContentType, header, read))
            throw new FileValidationException("File content does not match its declared type.");

        var blobPath = await _storage.SaveAsync(request.Content, request.FileName, request.ContentType, cancellationToken);

        try
        {
            var attachment = new FileAttachment(entityType, request.EntityId, request.FileName, request.ContentType, request.SizeBytes, blobPath);
            _files.Add(attachment);
            await _uow.SaveChangesAsync(cancellationToken);
            return attachment.ToDto();
        }
        catch
        {
            await _storage.DeleteAsync(blobPath, cancellationToken);
            throw;
        }
    }

    private static bool HasValidSignature(string contentType, byte[] header, int read)
    {
        if (!FileSignatures.TryGetValue(contentType.ToLowerInvariant(), out var sigs)) return true;
        foreach (var sig in sigs)
        {
            if (read < sig.Length) continue;
            var ok = true;
            for (int i = 0; i < sig.Length; i++) if (header[i] != sig[i]) { ok = false; break; }
            if (ok) return true;
        }
        return false;
    }
}
