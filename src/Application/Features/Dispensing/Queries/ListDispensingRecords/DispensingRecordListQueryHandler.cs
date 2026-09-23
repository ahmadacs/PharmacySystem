using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Dispensing.Dtos;
using Domain.Entities.Dispensing;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed class DispensingRecordListQueryHandler : IRequestHandler<DispensingRecordListQuery, Result<PagedList<DispensingRecordDto>>>
{
    private readonly IBaseRepository<DispensingRecord> _records;
    private readonly IStaffService _staff;

    public DispensingRecordListQueryHandler(IBaseRepository<DispensingRecord> records, IStaffService staff)
    {
        _records = records;
        _staff = staff;
    }

    public async Task<Result<PagedList<DispensingRecordDto>>> Handle(
        DispensingRecordListQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();

        var spec = new Specification<DispensingRecord, DispensingRecordRow>(r => new DispensingRecordRow(
                    r.Id,
                    r.PrescriptionId,
                    r.Prescription != null && r.Prescription.Patient != null ? r.Prescription.Patient.FullName : string.Empty,
                    r.PharmacistId,
                    r.DispensedAt,
                    r.Notes));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            spec.Where(r =>
                (r.Prescription != null && r.Prescription.Patient != null &&
                 (r.Prescription.Patient.FirstName.Contains(search) || r.Prescription.Patient.LastName.Contains(search))) ||
                (r.Notes != null && r.Notes.Contains(search)));
        }

        if (request.FromDate.HasValue)
            spec.Where(r => r.DispensedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            spec.Where(r => r.DispensedAt <= request.ToDate.Value);

        spec.Order(request.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? (Func<IQueryable<DispensingRecord>, IOrderedQueryable<DispensingRecord>>)(q => q.OrderByDescending(r => r.DispensedAt))
            : q => q.OrderBy(r => r.DispensedAt));

        var totalCount = await _records.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _records.ListAsync(spec, cancellationToken);

        var pharmacistIds = rows.Select(r => r.PharmacistId).Distinct().ToList();
        var pharmacistNamesById = await _staff.GetPharmacistNamesAsync(pharmacistIds, cancellationToken);

        // Same second query as before (items for the page only — no N+1),
        // expressed as a spec over the same set with SelectMany flattened in memory.
        var recordIds = rows.Select(r => r.Id).ToList();

        var itemsSpec = new Specification<DispensingRecord, IEnumerable<DispensingRecordItemRow>>(r => r.Items
            .Select(i => new DispensingRecordItemRow(
                r.Id,
                i.MedicineBatchId,
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                    ? i.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                    ? $"{i.MedicineBatch.MedicineVariant.Form} {i.MedicineBatch.MedicineVariant.Strength} {i.MedicineBatch.MedicineVariant.Unit}"
                    : string.Empty,
                i.MedicineBatch != null ? i.MedicineBatch.BatchNumber : string.Empty,
                i.Quantity.Value)));
        itemsSpec.Where(r => recordIds.Contains(r.Id));

        var itemsByRecord = recordIds.Count == 0
            ? new Dictionary<Guid, List<DispensingRecordItemDto>>()
            : (await _records.ListAsync(itemsSpec, cancellationToken))
                .SelectMany(x => x)
                .GroupBy(x => x.RecordId)
                .ToDictionary(g => g.Key, g => g.Select(i => i.ToDto()).ToList());

        var items = rows
            .Select(r => r.ToDto(
                pharmacistNamesById.GetValueOrDefault(r.PharmacistId, string.Empty),
                itemsByRecord.TryGetValue(r.Id, out var recItems) ? recItems : new List<DispensingRecordItemDto>()))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<DispensingRecordDto>>.Success(items);
    }
}
