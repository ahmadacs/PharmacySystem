using Domain.Common;

namespace Domain.Entities.Files;

public enum FileEntityType
{
    Medicine = 1,
    Prescription = 2
}

public class FileAttachment : BaseEntity
{
    public FileEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string BlobPath { get; private set; } = string.Empty;

    private FileAttachment() { }

    public FileAttachment(FileEntityType entityType, Guid entityId, string fileName, string contentType, long sizeBytes, string blobPath)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("Content type is required.", nameof(contentType));
        if (sizeBytes <= 0) throw new ArgumentException("Size must be positive.", nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(blobPath)) throw new ArgumentException("Blob path is required.", nameof(blobPath));

        EntityType = entityType;
        EntityId = entityId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        BlobPath = blobPath;
    }
}
