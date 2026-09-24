using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
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

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();

        // Pure single-entity spec: filter + ordering + paging + projection all
        // live here in the Application layer. The dispensed total aggregates
        // through the DispensingItems navigation (existing FK, no extra round
        // trip, no Include — navigations inside a Select need none).
        // COUNT + page = same 2 queries as before; the repository exposes only
        // the generic Get/List/CountAsync and never sees a DTO shape decision.
        var spec = new Specification<MedicineBatch, MedicineBatchRow>(b => new MedicineBatchRow(
                    b.Id,
                    b.MedicineVariant!.MedicineId,
                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.Name : "Unknown",
                    b.MedicineVariant!.Medicine != null ? b.MedicineVariant.Medicine.NameAr : null,
                    $"{b.MedicineVariant!.Form} {b.MedicineVariant!.Strength} {b.MedicineVariant!.Unit}",
                    b.BatchNumber,
                    b.ManufactureDate,
                    b.ExpiryDate,
                    b.QuantityReceived.Value,
                    b.QuantityAvailable.Value,
                    b.UnitCost.Amount,
                    b.SupplierName,
                    b.CreatedAt,
                    b.DispensingItems.Sum(i => (int?)i.Quantity.Value) ?? 0));

        if (request.MedicineId.HasValue)
            spec.Where(b => b.MedicineVariant!.MedicineId == request.MedicineId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var trimmed = request.Search.Trim();
            spec.Where(b => b.BatchNumber.Contains(trimmed));
        }
        if (expiryFrom.HasValue)
            spec.Where(b => b.ExpiryDate > expiryFrom.Value);
        if (expiryTo.HasValue)
            spec.Where(b => b.ExpiryDate <= expiryTo.Value);

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => q => q.OrderByDirection(b => b.QuantityAvailable.Value, request.SortDir),
            "batch" => q => q.OrderByDirection(b => b.BatchNumber, request.SortDir),
            _ => q => q.OrderByDirection(b => b.ExpiryDate, request.SortDir)
        });

        var totalCount = await _batches.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _batches.ListAsync(spec, cancellationToken);

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
