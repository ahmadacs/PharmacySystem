using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.AuditLog.Dtos;
using Domain.Entities.Audit;
using Domain.Entities.Patients;
using Domain.Entities.Prescriptions;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed class ListAuditEntriesQueryHandler : IRequestHandler<ListAuditEntriesQuery, Result<PagedList<AuditEntryDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IBaseRepository<AuditEntry> _audit;
    private readonly IUserManager _users;
    private readonly IBaseRepository<Patient> _patients;
    private readonly IStaffService _staff;

    public ListAuditEntriesQueryHandler(
        IBaseRepository<AuditEntry> audit,
        IUserManager users,
        IBaseRepository<Patient> patients,
        IStaffService staff)
    {
        _audit = audit;
        _users = users;
        _patients = patients;
        _staff = staff;
    }

    public async Task<Result<PagedList<AuditEntryDto>>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize(200);

        var spec = new Specification<AuditEntry, AuditEntry>(e => e);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var trimmed = request.Search.Trim();
            spec.Where(e => e.EntityName.Contains(trimmed));
        }

        if (request.Action.HasValue)
            spec.Where(e => e.Action == request.Action.Value);

        if (!string.IsNullOrWhiteSpace(request.Entity))
            spec.Where(e => e.EntityName == request.Entity);

        if (request.From.HasValue)
            spec.Where(e => e.ChangedAt >= request.From.Value);

        if (request.To.HasValue)
            spec.Where(e => e.ChangedAt <= request.To.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "entity" => SortDir(e => e.EntityName, request.SortDir),
            "action" => SortDir(e => e.Action, request.SortDir),
            // No author column without the join: stable date order instead.
            _ => SortDir(e => e.ChangedAt, request.SortDir)
        });

        var totalCount = await _audit.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var entries = await _audit.ListAsync(spec, cancellationToken);

        var changesById = entries.ToDictionary(e => e.Id, e => DeserializeChanges(e.ChangesJson));

        // One batched users lookup for author display names (keyed by ChangedBy).
        var userNames = await _users.GetDisplayNamesAsync(
            entries.Select(e => e.ChangedBy)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList(),
            cancellationToken);

        // Prescription entries: resolve DoctorId/PatientId change values to
        // display names with LINQ (batched: one staff lookup + one patient
        // query per page, no N+1). Other entities keep raw values.
        var prescriptionChanges = entries
            .Where(e => e.EntityName == nameof(Prescription))
            .SelectMany(e => changesById[e.Id])
            .ToList();

        var doctorIds = ChangeIdsFor(prescriptionChanges, nameof(Prescription.DoctorId));
        var patientIds = ChangeIdsFor(prescriptionChanges, nameof(Prescription.PatientId));

        var doctorNames = doctorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _staff.GetDoctorNamesAsync(doctorIds, cancellationToken);

        IReadOnlyDictionary<Guid, string> patientNames = new Dictionary<Guid, string>();
        if (patientIds.Count > 0)
        {
            var patientSpec = new Specification<Patient, Patient>(p => p);
            patientSpec.Where(p => patientIds.Contains(p.Id));
            patientNames = (await _patients.ListAsync(patientSpec, cancellationToken))
                .ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}".Trim());
        }

        var prescriptionEntryIds = entries
            .Where(e => e.EntityName == nameof(Prescription))
            .Select(e => e.Id)
            .ToHashSet();

        var items = entries
            .Select(e =>
            {
                var changes = changesById[e.Id];
                if (prescriptionEntryIds.Contains(e.Id))
                    changes = changes
                        .Select(c => c with
                        {
                            OldValueDisplay = ResolveDisplay(c.Property, c.OldValue, doctorNames, patientNames)
                                ?? c.OldValueDisplay,
                            NewValueDisplay = ResolveDisplay(c.Property, c.NewValue, doctorNames, patientNames)
                                ?? c.NewValueDisplay,
                        })
                        .ToList();

                return e.ToDto(
                    e.ChangedBy.HasValue ? userNames.GetValueOrDefault(e.ChangedBy.Value) : null,
                    changes);
            })
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<AuditEntryDto>>.Success(items);
    }

    /// <summary>Distinct referenced ids carried by one change property (LINQ).</summary>
    private static List<Guid> ChangeIdsFor(IEnumerable<AuditChangeDto> changes, string property)
        => changes
            .Where(c => c.Property == property)
            .SelectMany(c => new[] { c.OldValue, c.NewValue })
            .Select(v => Guid.TryParse(v, out var id) ? (Guid?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

    /// <summary>Display name for a DoctorId/PatientId change value (LINQ lookups).</summary>
    private static string? ResolveDisplay(
        string property,
        string? value,
        IReadOnlyDictionary<Guid, string> doctorNames,
        IReadOnlyDictionary<Guid, string> patientNames)
    {
        if (value is null || !Guid.TryParse(value, out var id))
            return null;

        if (property == nameof(Prescription.DoctorId))
            return doctorNames.GetValueOrDefault(id);

        if (property == nameof(Prescription.PatientId))
            return patientNames.GetValueOrDefault(id);

        return null;
    }

    private static Func<IQueryable<AuditEntry>, IOrderedQueryable<AuditEntry>> SortDir<TKey>(
        System.Linq.Expressions.Expression<Func<AuditEntry, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);

    private static IReadOnlyList<AuditChangeDto> DeserializeChanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<AuditChangeDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
