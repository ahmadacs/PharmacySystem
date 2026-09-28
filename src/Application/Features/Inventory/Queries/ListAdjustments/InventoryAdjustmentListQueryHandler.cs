using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class InventoryAdjustmentListQueryHandler : IRequestHandler<InventoryAdjustmentListQuery, Result<PagedList<InventoryAdjustmentDto>>>
{
    private readonly IBaseRepository<InventoryAdjustment> _repo;
    private readonly IUserManager _users;

    public InventoryAdjustmentListQueryHandler(IBaseRepository<InventoryAdjustment> repo, IUserManager users)
    {
        _repo = repo;
        _users = users;
    }

    public async Task<Result<PagedList<InventoryAdjustmentDto>>> Handle(
        InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken)
    {

        var selector = (System.Linq.Expressions.Expression<Func<InventoryAdjustment, InventoryAdjustmentRow>>)(a => new InventoryAdjustmentRow(
                    a.Id,
                    a.MedicineBatchId,
                    a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null && a.MedicineBatch.MedicineVariant.Medicine != null
                        ? a.MedicineBatch.MedicineVariant.Medicine.Name : "Unknown",
                    a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null && a.MedicineBatch.MedicineVariant.Medicine != null
                        ? a.MedicineBatch.MedicineVariant.Medicine.NameAr : null,
                    a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null ? a.MedicineBatch.MedicineVariant.Form : null,
                    a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null ? a.MedicineBatch.MedicineVariant.Unit : null,
                    a.MedicineBatch != null && a.MedicineBatch.MedicineVariant != null ? a.MedicineBatch.MedicineVariant.Strength : null,
                    a.MedicineBatch != null ? a.MedicineBatch.BatchNumber : string.Empty,
                    a.Type,
                    a.QuantityChanged,
                    a.QuantityBefore,
                    a.QuantityAfter,
                    a.Reason,
                    a.AdjustedBy,
                    a.AdjustedAt));

        var type = request.Type;
        var trimmed = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        System.Linq.Expressions.Expression<Func<InventoryAdjustment, bool>> predicate =
            a => (!type.HasValue || a.Type == type.Value)
                && (trimmed == null || a.Reason.Contains(trimmed));

        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();
        var desc = request.SortDir.IsDescending();

        var totalCount = await _repo.CountAsync(predicate, cancellationToken);
        List<InventoryAdjustmentRow> rows = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => await _repo.PagedAsync(selector, predicate, a => a.QuantityChanged, desc, page, pageSize, cancellationToken),
            _ => await _repo.PagedAsync(selector, predicate, a => a.AdjustedAt, desc, page, pageSize, cancellationToken)
        };

        var userNames = await _users.GetDisplayNamesAsync(
            rows.Where(r => r.AdjustedBy.HasValue).Select(r => r.AdjustedBy!.Value).Distinct().ToList(),
            cancellationToken);

        var items = rows
            .Select(r => r.ToDto(r.AdjustedBy.HasValue ? userNames.GetValueOrDefault(r.AdjustedBy.Value) : null))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<InventoryAdjustmentDto>>.Success(items);
    }
}
