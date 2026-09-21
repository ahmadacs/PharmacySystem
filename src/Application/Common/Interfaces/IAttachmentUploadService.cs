using Application.Features.Inventory.Dtos;

namespace Application.Common.Interfaces;

/// <summary>
/// Persists an optional base64 file attachment carried inside create/update
/// requests. Null or empty payloads are no-ops, so call sites stay one line.
/// </summary>
public interface IAttachmentUploadService
{
    Task UploadAsync(
        string entityType,
        Guid entityId,
        FileUploadDto? file,
        CancellationToken cancellationToken = default);
}
