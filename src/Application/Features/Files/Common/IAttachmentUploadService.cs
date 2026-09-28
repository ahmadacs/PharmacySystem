using Application.Features.Files.Dtos;

namespace Application.Features.Files.Common;

public interface IAttachmentUploadService
{
    Task UploadAsync(
        string entityType,
        Guid entityId,
        FileUploadDto? file,
        CancellationToken cancellationToken = default);
}
