using System.Globalization;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Common.Security;
using Application.Features.Files.Dtos;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Exceptions;
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

        // Authorization per entity type
        if (entityType == FileEntityType.Medicine)
        {
            var hasPerm = _currentUser.Permissions.Contains(Permissions.Medicines.Create) || _currentUser.Permissions.Contains(Permissions.Medicines.Update);
            if (!hasPerm) return Result<FileAttachmentDto>.Failure(_localizer["FileUploadMedicine"].Value, 403);
            var exists = await _files.MedicineExistsAsync(request.EntityId, cancellationToken);
            if (!exists) return Result<FileAttachmentDto>.Failure(_localizer["ResourceNotFound", "Medicine", request.EntityId].Value, 404);
        }
        else if (entityType == FileEntityType.Prescription)
        {
            var prescription = await _prescriptions.GetByIdAsync(request.EntityId, cancellationToken);
            if (prescription is null) return Result<FileAttachmentDto>.Failure(_localizer["ResourceNotFound", "Prescription", request.EntityId].Value, 404);
            try
            {
                await _resourceAuth.EnsureCanAccessPrescriptionAsync(prescription, PrescriptionOperation.View, cancellationToken);
            }
            catch (ForbiddenResourceException ex)
            {
                return Result<FileAttachmentDto>.Failure(ex.Message, 403);
            }
        }
        else if (entityType == FileEntityType.Batch)
        {
            var hasPerm = _currentUser.Permissions.Contains(Permissions.Inventory.View) || _currentUser.Permissions.Contains(Permissions.Inventory.Adjust);
            if (!hasPerm) return Result<FileAttachmentDto>.Failure(_localizer["FileUploadBatch"].Value, 403);
            var exists = await _files.BatchExistsAsync(request.EntityId, cancellationToken);
            if (!exists) return Result<FileAttachmentDto>.Failure(_localizer["ResourceNotFound", "MedicineBatch", request.EntityId].Value, 404);
        }
        else if (entityType == FileEntityType.InventoryAdjustment)
        {
            var hasPerm = _currentUser.Permissions.Contains(Permissions.Inventory.Adjust);
            if (!hasPerm) return Result<FileAttachmentDto>.Failure(_localizer["FileUploadInventory"].Value, 403);
            var exists = await _files.InventoryAdjustmentExistsAsync(request.EntityId, cancellationToken);
            if (!exists) return Result<FileAttachmentDto>.Failure(_localizer["ResourceNotFound", "InventoryAdjustment", request.EntityId].Value, 404);
        }
        else
        {
            return Result<FileAttachmentDto>.Failure(_localizer["InvalidEntityType", entityType, "Medicine, Prescription, Batch, InventoryAdjustment"].Value, 422);
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
        catch (DomainException ex)
        {
            await _storage.DeleteAsync(blobPath, cancellationToken);
            return Result<FileAttachmentDto>.Failure(ex.Message, 422);
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
