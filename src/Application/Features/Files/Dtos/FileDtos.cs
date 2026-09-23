using Domain.Enums;

namespace Application.Features.Files.Dtos;

public sealed record FileAttachmentDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string BlobPath,
    DateTime CreatedAt
);

public static class FileAttachmentMapping
{
    public static Domain.Entities.Files.FileAttachment ToEntity(
        FileEntityType entityType,
        Guid entityId,
        string fileName,
        string contentType,
        long sizeBytes,
        string blobPath)
        => new(entityType, entityId, fileName, contentType, sizeBytes, blobPath);

    public static FileAttachmentDto ToDto(this Domain.Entities.Files.FileAttachment e) => new(
        e.Id,
        e.EntityType.ToString(),
        e.EntityId,
        e.FileName,
        e.ContentType,
        e.SizeBytes,
        e.BlobPath,
        e.CreatedAt
    );
}
