using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class BatchListQueryHandler : IRequestHandler<BatchListQuery, Result<PagedList<MedicineBatchDto>>>
{
    private readonly IRepository<MedicineBatch> _batches;

    public BatchListQueryHandler(IRepository<MedicineBatch> batches)
    {
        _batches = batches;
    }

    public async Task<Result<PagedList<MedicineBatchDto>>> Handle(BatchListQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var (expiryFrom, expiryTo) = GetExpiryRange(request.ExpiryStatus, asOf, request.WithinDays);

        var medicineId = request.MedicineId;
        var trimmed = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        System.Linq.Expressions.Expression<Func<MedicineBatch, bool>> predicate =
            b => (!medicineId.HasValue || b.MedicineVariant!.MedicineId == medicineId.Value)
                && (trimmed == null || b.BatchNumber.Contains(trimmed))
                && (!expiryFrom.HasValue || b.ExpiryDate > expiryFrom.Value)
                && (!expiryTo.HasValue || b.ExpiryDate <= expiryTo.Value);

        var paged = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => await _batches.PagedAsync(BatchRowProjection, predicate, b => b.QuantityAvailable.Value, request.ToPagination(), cancellationToken),
            "batch" => await _batches.PagedAsync(BatchRowProjection, predicate, b => b.BatchNumber, request.ToPagination(), cancellationToken),
            _ => await _batches.PagedAsync(BatchRowProjection, predicate, b => b.ExpiryDate, request.ToPagination(), cancellationToken)
        };

        var items = paged.Select(r => r.ToDto(asOf));

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

    private static readonly Expression<Func<MedicineBatch, MedicineBatchRow>> BatchRowProjection = b => new MedicineBatchRow(
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
        b.DispensingItems.Sum(i => (int?)i.Quantity.Value) ?? 0);
}
