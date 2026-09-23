using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Inventory;
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
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize();

        var spec = new Specification<InventoryAdjustment, InventoryAdjustmentRow>(a => new InventoryAdjustmentRow(
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

        if (request.Type.HasValue)
            spec.Where(a => a.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var trimmed = request.Search.Trim();
            spec.Where(a => a.Reason.Contains(trimmed));
        }

        spec.Order(request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => SortDir(a => a.QuantityChanged, request.SortDir),
            _ => SortDir(a => a.AdjustedAt, request.SortDir)
        });

        var totalCount = await _repo.CountAsync(spec, cancellationToken);

        spec.Page((page - 1) * pageSize, pageSize);

        var rows = await _repo.ListAsync(spec, cancellationToken);

        var adjustedByIds = rows
            .Where(r => r.AdjustedBy.HasValue)
            .Select(r => r.AdjustedBy!.Value)
            .Distinct()
            .ToList();
        var userNames = await _users.GetDisplayNamesAsync(adjustedByIds, cancellationToken);

        var items = rows
            .Select(r => r.ToDto(r.AdjustedBy.HasValue ? userNames.GetValueOrDefault(r.AdjustedBy.Value) : null))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<InventoryAdjustmentDto>>.Success(items);
    }

    private static Func<IQueryable<InventoryAdjustment>, IOrderedQueryable<InventoryAdjustment>> SortDir<TKey>(
        System.Linq.Expressions.Expression<Func<InventoryAdjustment, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q => q.OrderByDescending(keySelector)
            : q => q.OrderBy(keySelector);
}
