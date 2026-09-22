using Application.Common.Models;
using Domain.Entities.Audit;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IAuditRepository
{
    /// <summary>
    /// Searches audit entries with SQL-side filtering, sorting and paging.
    /// Author names ride along from the same query (the users join is needed
    /// for searching/sorting anyway), so callers must NOT re-query users.
    /// JSON deserialization and DTO mapping happen in the Application handler.
    /// </summary>
    Task<PagedList<AuditEntryWithAuthor>> ListAsync(
        PagedQuery paging,
        AuditAction? action,
        string? entity,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}

/// <summary>An audit entry plus its author's display name (null = system).</summary>
public sealed record AuditEntryWithAuthor(AuditEntry Entry, string? AuthorName);