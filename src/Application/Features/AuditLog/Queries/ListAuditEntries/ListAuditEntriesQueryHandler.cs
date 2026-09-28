using System.Text.Json;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
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
        var desc = request.SortDir.IsDescending();

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var entity = request.Entity;
        var action = request.Action;
        var from = request.From;
        var to = request.To;
        System.Linq.Expressions.Expression<Func<AuditEntry, bool>> predicate =
            e => (search == null || e.EntityName.Contains(search))
                && (!action.HasValue || e.Action == action.Value)
                && (entity == null || e.EntityName == entity)
                && (!from.HasValue || e.ChangedAt >= from.Value)
                && (!to.HasValue || e.ChangedAt <= to.Value);

        var totalCount = await _audit.CountAsync(predicate, cancellationToken);

        List<AuditEntry> entries = request.SortBy?.ToLowerInvariant() switch
        {
            "entity" => await _audit.PagedAsync(e => e, predicate, e => e.EntityName, desc, page, pageSize, cancellationToken),
            "action" => await _audit.PagedAsync(e => e, predicate, e => e.Action, desc, page, pageSize, cancellationToken),

            _ => await _audit.PagedAsync(e => e, predicate, e => e.ChangedAt, desc, page, pageSize, cancellationToken)
        };

        var changesById = entries.ToDictionary(e => e.Id, e => DeserializeChanges(e.ChangesJson));

        var userNames = await _users.GetDisplayNamesAsync(
            entries.Select(e => e.ChangedBy)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList(),
            cancellationToken);

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
            patientNames = (await _patients.ListAsync(p => new { p.Id, FullName = p.FirstName + " " + p.LastName }, p => patientIds.Contains(p.Id), cancellationToken: cancellationToken))
                .ToDictionary(p => p.Id, p => p.FullName.Trim());
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

    private static List<Guid> ChangeIdsFor(IEnumerable<AuditChangeDto> changes, string property)
        => changes
            .Where(c => c.Property == property)
            .SelectMany(c => new[] { c.OldValue, c.NewValue })
            .Select(v => Guid.TryParse(v, out var id) ? (Guid?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

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
