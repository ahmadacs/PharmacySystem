using Application.Common.Models;
using Domain.Entities.Files;
using Domain.Enums;

namespace Application.Features.Files.Common;

public interface IFileAccessChecker
{

    Task<Result?> EnsureCanAttachAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken);

    Task<Result?> EnsureCanViewAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken);

    Task<Result?> EnsureCanViewAsync(FileAttachment attachment, CancellationToken cancellationToken);
}
