using Application.Common.Models;
using Domain.Entities.Files;
using Domain.Enums;

namespace Application.Features.Files.Common;

/// <summary>
/// Single place for file-attachment authorization: permission check first,
/// then entity-existence check. Null means allowed, otherwise the failure
/// to return to the caller.
/// </summary>
public interface IFileAccessChecker
{
    /// <summary>Upload path: attach permissions + existence check.</summary>
    Task<Result?> EnsureCanAttachAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken);

    /// <summary>List path: view permissions + existence check (404 when missing).</summary>
    Task<Result?> EnsureCanViewAsync(FileEntityType entityType, Guid entityId, CancellationToken cancellationToken);

    /// <summary>
    /// Download path: view permissions on an already-loaded attachment, no
    /// existence check. A prescription that no longer exists is allowed
    /// through (orphaned file stays downloadable) — preserved behavior.
    /// </summary>
    Task<Result?> EnsureCanViewAsync(FileAttachment attachment, CancellationToken cancellationToken);
}
