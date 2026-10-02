using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using Domain.Entities.Dispensing;
using MediatR;
using System.Linq.Expressions;

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

        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var fromDate = request.FromDate;
        var toDate = request.ToDate;
        Expression<Func<DispensingRecord, bool>> predicate =
            r => (search == null ||
                    ((r.Prescription != null && r.Prescription.Patient != null &&
                      (r.Prescription.Patient.FirstName.Contains(search) || r.Prescription.Patient.LastName.Contains(search))) ||
                      (r.Notes != null && r.Notes.Contains(search))))
                && (!fromDate.HasValue || r.DispensedAt >= fromDate.Value)
                && (!toDate.HasValue || r.DispensedAt <= toDate.Value);

        var paged = await _records.PagedAsync(
            RecordWithItemsProjection, predicate, r => r.DispensedAt,
            request.ToPagination(), cancellationToken);

        var pharmacistNamesById = await _staff.GetPharmacistNamesAsync(
            paged.Items.Select(x => x.Row.PharmacistId).Distinct().ToList(), cancellationToken);

        var itemsByRecord = paged.Items
            .SelectMany(x => x.Items)
            .GroupBy(x => x.RecordId)
            .ToDictionary(g => g.Key, g => g.Select(i => i.ToDto()).ToList());

        var items = paged.Select(x => x.Row.ToDto(
                pharmacistNamesById.GetValueOrDefault(x.Row.PharmacistId, string.Empty),
                itemsByRecord.TryGetValue(x.Row.Id, out var recItems) ? recItems : new List<DispensingRecordItemDto>()));

        return Result<PagedList<DispensingRecordDto>>.Success(items);
    }

    private static readonly Expression<Func<DispensingRecord, DispensingRecordWithItems>> RecordWithItemsProjection = r => new DispensingRecordWithItems(
        new DispensingRecordRow(
            r.Id,
            r.PrescriptionId,
            r.Prescription != null && r.Prescription.Patient != null
                ? (r.Prescription.Patient.FirstName + " " + r.Prescription.Patient.LastName) : string.Empty,
            r.PharmacistId,
            r.DispensedAt,
            r.Notes),
        r.Items
            .Select(i => new DispensingRecordItemRow(
                r.Id,
                i.MedicineBatchId,
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                    ? i.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null && i.MedicineBatch.MedicineVariant.Medicine != null
                    ? i.MedicineBatch.MedicineVariant.Medicine.NameAr : null,
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                    ? (Domain.Enums.MedicineForm?)i.MedicineBatch.MedicineVariant.Form : null,
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                    ? (Domain.Enums.MedicineUnit?)i.MedicineBatch.MedicineVariant.Unit : null,
                i.MedicineBatch != null && i.MedicineBatch.MedicineVariant != null
                    ? (decimal?)i.MedicineBatch.MedicineVariant.Strength : null,
                i.MedicineBatch != null ? i.MedicineBatch.BatchNumber : string.Empty,
                i.Quantity.Value))
            .ToList());
}
