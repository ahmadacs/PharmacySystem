using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class BatchListQueryHandler : IRequestHandler<BatchListQuery, Result<PagedList<MedicineBatchDto>>>
{
    private readonly IBaseRepository<MedicineBatch> _batches;

    public BatchListQueryHandler(IBaseRepository<MedicineBatch> batches)
    {
        _batches = batches;
    }

    public async Task<Result<PagedList<MedicineBatchDto>>> Handle(BatchListQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var (expiryFrom, expiryTo) = GetExpiryRange(request.ExpiryStatus, asOf, request.WithinDays);


        // Pure single-entity spec: filter + ordering + paging + projection all
        // live here in the Application layer. The dispensed total aggregates
        // through the DispensingItems navigation (existing FK, no extra round
        // trip, no Include — navigations inside a Select need none).
        // Scientific name (Medicine.Name) + variant type (Form/Strength/Unit)
        // are scalar columns; VariantName is built in MedicineMapping (not SQL).
        var selector = (System.Linq.Expressions.Expression<Func<MedicineBatch, MedicineBatchRow>>)(b => new MedicineBatchRow(
                    b.Id,
                    b.MedicineVariant!.MedicineId,
                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.Name : "Unknown",
                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.NameAr : null,
                    b.MedicineVariant!.Form,
                    b.MedicineVariant!.Unit,
                    b.MedicineVariant!.Strength,
                    b.BatchNumber,
                    b.ManufactureDate,
                    b.ExpiryDate,
                    b.QuantityReceived.Value,
                    b.QuantityAvailable.Value,
                    b.UnitCost.Amount,
                    b.SupplierName,
                    b.CreatedAt,
                    b.DispensingItems.Sum(i => (int?)i.Quantity.Value) ?? 0));

        var medicineId = request.MedicineId;
        var trimmed = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        System.Linq.Expressions.Expression<Func<MedicineBatch, bool>> predicate =
            b => (!medicineId.HasValue || b.MedicineVariant!.MedicineId == medicineId.Value)
                && (trimmed == null || b.BatchNumber.Contains(trimmed))
                && (!expiryFrom.HasValue || b.ExpiryDate > expiryFrom.Value)
                && (!expiryTo.HasValue || b.ExpiryDate <= expiryTo.Value);

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();
        var desc = request.SortDir.IsDescending();

        var totalCount = await _batches.CountAsync(predicate, cancellationToken);
        List<MedicineBatchRow> rows = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => await _batches.PagedAsync(selector, predicate, b => b.QuantityAvailable.Value, desc, page, pageSize, cancellationToken),
            "batch" => await _batches.PagedAsync(selector, predicate, b => b.BatchNumber, desc, page, pageSize, cancellationToken),
            _ => await _batches.PagedAsync(selector, predicate, b => b.ExpiryDate, desc, page, pageSize, cancellationToken)
        };

        var items = rows
            .Select(r => r.ToDto(asOf))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<MedicineBatchDto>>.Success(items);
    }

    private static (DateOnly? From, DateOnly? To) GetExpiryRange(string? expiryStatus, DateOnly asOf, int withinDays)
        => expiryStatus?.ToLowerInvariant() switch
        {
            "valid" => (asOf, null),
            "expirings" or "expiringsoon" => (asOf, asOf.AddDays(withinDays)),
            "expired" => (null, asOf),
            _ => (null, null)
        };
}
