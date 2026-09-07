using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using Domain.Entities.Dispensing;
using MediatR;

namespace Application.Features.Dispensing.Queries;

public sealed class DispensingRecordListQueryHandler : IRequestHandler<DispensingRecordListQuery, Result<PagedList<DispensingRecordDto>>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IStaffService _staff;
    private readonly IAsyncQueryExecutor _executor;

    public DispensingRecordListQueryHandler(IPrescriptionRepository prescriptions, IStaffService staff, IAsyncQueryExecutor executor)
    {
        _prescriptions = prescriptions;
        _staff = staff;
        _executor = executor;
    }

    public async Task<Result<PagedList<DispensingRecordDto>>> Handle(
        DispensingRecordListQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<DispensingRecord> data = _prescriptions.QueryDispensingRecords();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            data = data.Where(r =>
                (r.Prescription != null && r.Prescription.Patient != null &&
                 (r.Prescription.Patient.FirstName.Contains(search) || r.Prescription.Patient.LastName.Contains(search))) ||
                (r.Notes != null && r.Notes.Contains(search)));
        }

        if (request.FromDate.HasValue)
            data = data.Where(r => r.DispensedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            data = data.Where(r => r.DispensedAt <= request.ToDate.Value);

        var totalCount = await _executor.CountAsync(data, cancellationToken);

        data = SortDir(data, r => r.DispensedAt, request.SortDir);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await _executor.ToListAsync(
            data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new DispensingRecordRow(
                    r.Id,
                    r.PrescriptionId,
                    r.Prescription != null && r.Prescription.Patient != null ? r.Prescription.Patient.FullName : string.Empty,
                    r.PharmacistId,
                    r.DispensedAt,
                    r.Notes)),
            cancellationToken);

        var pharmacistIds = rows.Select(r => r.PharmacistId).Distinct().ToList();
        var pharmacistNamesById = await _staff.GetPharmacistNamesAsync(pharmacistIds, cancellationToken);

        // Fetch items for the page in a separate query to avoid correlated subqueries per row
        var recordIds = rows.Select(r => r.Id).ToList();
        var itemsByRecord = recordIds.Count == 0
            ? new Dictionary<Guid, List<DispensingRecordItemDto>>()
            : (await _executor.ToListAsync(
                _prescriptions.QueryDispensingRecords()
                    .Where(r => recordIds.Contains(r.Id))
                    .SelectMany(r => r.Items.Select(i => new DispensingRecordItemRow(
                        r.Id,
                        i.MedicineBatchId,
                        i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                            ? i.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                        i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                            ? $"{i.MedicineBatch.MedicineVariant.Form} {i.MedicineBatch.MedicineVariant.Strength} {i.MedicineBatch.MedicineVariant.Unit}"
                            : string.Empty,
                        i.MedicineBatch != null ? i.MedicineBatch.BatchNumber : string.Empty,
                        i.Quantity.Value))),
                cancellationToken))
                .GroupBy(x => x.RecordId)
                .ToDictionary(g => g.Key, g => g.Select(i => i.ToDto()).ToList());

        var items = rows
            .Select(r => r.ToDto(
                pharmacistNamesById.GetValueOrDefault(r.PharmacistId, string.Empty),
                itemsByRecord.TryGetValue(r.Id, out var recItems) ? recItems : new List<DispensingRecordItemDto>()))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<DispensingRecordDto>>.Success(items);
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
