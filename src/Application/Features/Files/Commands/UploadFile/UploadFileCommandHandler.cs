using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Common.Security;
using Application.Features.Files.Dtos;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Application.Features.Files.Commands.UploadFile;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileAttachmentDto>>
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

    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["application/pdf"] = [".pdf"]
    };

    private const long DefaultMaxSize = 5 * 1024 * 1024;

    private readonly IFileAttachmentRepository _files;
    private readonly IFileStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly FileStorageOptions _options;
    private readonly ICurrentUserService _currentUser;
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IResourceAuthorizationService _resourceAuth;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UploadFileCommandHandler(IFileAttachmentRepository files, IFileStorageService storage, IUnitOfWork uow, IOptions<FileStorageOptions> options, ICurrentUserService currentUser, IPrescriptionRepository prescriptions, IResourceAuthorizationService resourceAuth, IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
        _storage = storage;
        _uow = uow;
        _options = options.Value;
        _currentUser = currentUser;
        _prescriptions = prescriptions;
        _resourceAuth = resourceAuth;
        _localizer = localizer;
    }

    public async Task<Result<FileAttachmentDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<FileAttachmentDto>.Failure(_localizer["FileTypeNotAllowed", request.ContentType].Value, 422);

        var maxSize = _options.MaxFileSizeBytes > 0 ? _options.MaxFileSizeBytes : DefaultMaxSize;
        if (request.SizeBytes > maxSize)
            return Result<FileAttachmentDto>.Failure(_localizer["FileSizeExceeds",
                request.SizeBytes.ToString(CultureInfo.InvariantCulture),
                maxSize.ToString(CultureInfo.InvariantCulture)].Value, 422);

        if (request.SizeBytes <= 0)
            return Result<FileAttachmentDto>.Failure(_localizer["FileEmpty"].Value, 422);

        if (!Enum.TryParse<FileEntityType>(request.EntityType, true, out var entityType))
            return Result<FileAttachmentDto>.Failure(_localizer["InvalidEntityType", request.EntityType, "Medicine, Prescription, Batch, InventoryAdjustment"].Value, 422);

        // One authorization gate per entity type (null = allowed).
        var authFailure = await AuthorizeAsync(entityType, request.EntityId, cancellationToken);
        if (authFailure is not null)
            return authFailure;

        // Extension must match the (already validated) content type.
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!AllowedExtensions[request.ContentType].Contains(ext))
            return Result<FileAttachmentDto>.Failure(_localizer["ExtensionMismatch", ext, request.ContentType].Value, 422);

        var header = new byte[8];
        request.Content.Position = 0;
        var read = await request.Content.ReadAsync(header, 0, 8, cancellationToken);
        request.Content.Position = 0;
        if (!HasValidSignature(request.ContentType, header, read))
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

    /// <summary>
    /// Returns a failure when the current user may not attach to the entity,
    /// or null when allowed. Existence is checked first so unknown ids are 404.
    /// </summary>
    private async Task<Result<FileAttachmentDto>?> AuthorizeAsync(
        FileEntityType entityType, Guid entityId, CancellationToken cancellationToken)
    {
        if (entityType == FileEntityType.Prescription)
        {
            var prescription = await _prescriptions.GetByIdAsync(entityId, cancellationToken);
            if (prescription is null)
                return Result<FileAttachmentDto>.Failure(_localizer["ResourceNotFound", "Prescription", entityId].Value, 404);
            await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
            return null;
        }

        var (permissions, exists, resource, messageKey) = entityType switch
        {
            FileEntityType.Medicine => (
                (IReadOnlyList<string>)[Permissions.Medicines.Create, Permissions.Medicines.Update],
                _files.MedicineExistsAsync(entityId, cancellationToken),
                "Medicine",
                "FileUploadMedicine"),
            FileEntityType.Batch => (
                (IReadOnlyList<string>)[Permissions.Inventory.View, Permissions.Inventory.Adjust],
                _files.BatchExistsAsync(entityId, cancellationToken),
                "MedicineBatch",
                "FileUploadBatch"),
            _ => (
                (IReadOnlyList<string>)[Permissions.Inventory.Adjust],
                _files.InventoryAdjustmentExistsAsync(entityId, cancellationToken),
                "InventoryAdjustment",
                "FileUploadInventory")
        };

        if (!permissions.Any(p => _currentUser.Permissions.Contains(p)))
            return Result<FileAttachmentDto>.Failure(_localizer[messageKey].Value, 403);
        if (!await exists)
            return Result<FileAttachmentDto>.Failure(_localizer["ResourceNotFound", resource, entityId].Value, 404);
        return null;
    }

    private static bool HasValidSignature(string contentType, byte[] header, int read)
    {
        if (!FileSignatures.TryGetValue(contentType.ToLowerInvariant(), out var sigs)) return true;
        return sigs.Any(sig => read >= sig.Length && header.Take(sig.Length).SequenceEqual(sig));
    }
}
