using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Files.Common;
using Application.Features.Files.Dtos;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Application.Features.Files.Commands.UploadFile;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileAttachmentDto>>
{
    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["application/pdf"] = [".pdf"],
    };

    private static readonly Dictionary<string, byte[]> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        ["application/pdf"] = [0x25, 0x50, 0x44, 0x46],
    };

    private const long DefaultMaxSize = 5 * 1024 * 1024;

    private readonly IBaseRepository<FileAttachment> _files;
    private readonly IFileStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly IFileAccessChecker _access;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly FileStorageOptions _options;

    public UploadFileCommandHandler(
        IBaseRepository<FileAttachment> files,
        IFileStorageService storage,
        IUnitOfWork uow,
        IOptions<FileStorageOptions> options,
        IFileAccessChecker access,
        IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
        _storage = storage;
        _uow = uow;
        _options = options.Value;
        _access = access;
        _localizer = localizer;
    }

    public async Task<Result<FileAttachmentDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        // 1. File type must be known.
        if (!AllowedExtensions.ContainsKey(request.ContentType))
            return Result<FileAttachmentDto>.Failure(_localizer["FileTypeNotAllowed", request.ContentType].Value, 422);

        // 2. Size limits.
        if (request.SizeBytes <= 0)
            return Result<FileAttachmentDto>.Failure(_localizer["FileEmpty"].Value, 422);

        var maxSize = _options.MaxFileSizeBytes > 0 ? _options.MaxFileSizeBytes : DefaultMaxSize;
        if (request.SizeBytes > maxSize)
            return Result<FileAttachmentDto>.Failure(_localizer["FileSizeExceeds",
                request.SizeBytes.ToString(CultureInfo.InvariantCulture),
                maxSize.ToString(CultureInfo.InvariantCulture)].Value, 422);

        // 3. Entity type must be valid.
        if (!Enum.TryParse<FileEntityType>(request.EntityType, true, out var entityType))
            return Result<FileAttachmentDto>.Failure(
                _localizer["InvalidEntityType", request.EntityType, "Medicine, Prescription, Batch, InventoryAdjustment"].Value, 422);

        // 4. Caller must be allowed to attach to this entity.
        var accessFailure = await _access.EnsureCanAttachAsync(entityType, request.EntityId, cancellationToken);
        if (accessFailure is not null)
            return Result<FileAttachmentDto>.Failure(accessFailure.Error!, accessFailure.StatusCode);

        // 5. Extension must match content type, and content must match its magic bytes.
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!AllowedExtensions[request.ContentType].Contains(ext))
            return Result<FileAttachmentDto>.Failure(_localizer["ExtensionMismatch", ext, request.ContentType].Value, 422);

        request.Content.Position = 0;
        var header = new byte[8];
        var read = await request.Content.ReadAsync(header, 0, 8, cancellationToken);
        request.Content.Position = 0;

        var signature = FileSignatures[request.ContentType];
        if (read < signature.Length || !header.Take(signature.Length).SequenceEqual(signature))
            return Result<FileAttachmentDto>.Failure(_localizer["ContentMismatch"].Value, 422);

        var blobPath = await _storage.SaveAsync(request.Content, request.FileName, request.ContentType, cancellationToken);
        try
        {
            var attachment = FileAttachmentMapping.ToEntity(entityType, request.EntityId, request.FileName, request.ContentType, request.SizeBytes, blobPath);
            _files.Add(attachment);
            await _uow.SaveChangesAsync(cancellationToken);
            return Result<FileAttachmentDto>.Success(attachment.ToDto());
        }
        catch
        {
            await _storage.DeleteAsync(blobPath, cancellationToken);
            throw;
        }
    }
}
