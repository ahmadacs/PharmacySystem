namespace Application.Features.Files.Dtos;

public sealed record FileAttachmentDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string BlobPath,
    DateTime CreatedAt,
    string? Url
);

public static class FileAttachmentMapping
{
    public static Domain.Entities.Files.FileAttachment ToEntity(
        Domain.Entities.Files.FileEntityType entityType,
        Guid entityId,
        string fileName,
        string contentType,
        long sizeBytes,
        string blobPath)
        => new(entityType, entityId, fileName, contentType, sizeBytes, blobPath);

    public static FileAttachmentDto ToDto(this Domain.Entities.Files.FileAttachment e, string? baseUrl = null) => new(
        e.Id,
        e.EntityType.ToString(),
        e.EntityId,
        e.FileName,
        e.ContentType,
        e.SizeBytes,
        e.BlobPath,
        e.CreatedAt,
        baseUrl is null ? null : $"{baseUrl.TrimEnd('/')}/{e.BlobPath}"
    );
}
