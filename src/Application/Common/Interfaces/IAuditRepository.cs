using Application.Common.Models;
using Domain.Entities.Audit;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IAuditRepository
{
    /// <summary>
    /// Searches audit entries with SQL-side filtering, sorting and paging.
    /// Returns entities; author-name resolution, JSON deserialization and DTO
    /// mapping happen in the Application handler.
    /// </summary>
    Task<PagedList<AuditEntry>> ListAsync(
        PagedQuery paging,
        AuditAction? action,
        string? entity,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}