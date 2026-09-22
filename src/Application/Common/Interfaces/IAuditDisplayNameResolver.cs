using Domain.Entities.Audit;

namespace Application.Common.Interfaces;

/// <summary>
/// Resolves every human-readable name an audit page needs in one batched pass:
/// entry target labels, foreign-key change values (e.g. a <c>PatientId</c> of
/// <c>"…guid…"</c> becomes <c>"Sara Ahmed"</c>) and user names. Ids are merged
/// per entity type so each table is queried at most once and users exactly
/// once. Missing keys mean "no display found" — the caller keeps raw values.
/// </summary>
public interface IAuditDisplayNameResolver
{
    Task<AuditDisplayResult> ResolvePageAsync(
        IReadOnlyList<AuditEntry> entries,
        IReadOnlyCollection<AuditValueRef> valueRefs,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}

/// <summary>All display names for one audit page.</summary>
/// <param name="EntryDisplays">Keyed by <see cref="AuditEntry.Id"/>.</param>
/// <param name="ValueDisplays">Resolved FK change values.</param>
/// <param name="UserNames">Keyed by user id.</param>
public sealed record AuditDisplayResult(
    IReadOnlyDictionary<Guid, string> EntryDisplays,
    IReadOnlyDictionary<AuditValueRef, string> ValueDisplays,
    IReadOnlyDictionary<Guid, string> UserNames);

/// <summary>A single change value that may reference another entity.</summary>
/// <param name="Property">The change property name, e.g. <c>PatientId</c>.</param>
/// <param name="ValueId">The referenced entity id.</param>
public readonly record struct AuditValueRef(string Property, Guid ValueId);
