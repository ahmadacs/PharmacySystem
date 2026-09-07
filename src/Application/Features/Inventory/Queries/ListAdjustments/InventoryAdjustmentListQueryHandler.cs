using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Inventory;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed class InventoryAdjustmentListQueryHandler : IRequestHandler<InventoryAdjustmentListQuery, Result<PagedList<InventoryAdjustmentDto>>>
{
    private readonly IMedicineRepository _repo;
    private readonly IUserManager _users;
    private readonly IAsyncQueryExecutor _executor;

    public InventoryAdjustmentListQueryHandler(IMedicineRepository repo, IUserManager users, IAsyncQueryExecutor executor)
    {
        _repo = repo;
        _users = users;
        _executor = executor;
    }

    public async Task<Result<PagedList<InventoryAdjustmentDto>>> Handle(
        InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<InventoryAdjustment> data = _repo.QueryAdjustments();

        if (request.Type.HasValue)
            data = data.Where(a => a.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            data = data.Where(a => a.Reason.Contains(request.Search.Trim()));

        var totalCount = await _executor.CountAsync(data, cancellationToken);

        data = request.SortBy?.ToLowerInvariant() switch
        {
            "quantity" => SortDir(data, a => a.QuantityChanged, request.SortDir),
            _ => SortDir(data, a => a.AdjustedAt, request.SortDir)
        };

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await _executor.ToListAsync(
            data
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new InventoryAdjustmentRow(
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
                    a.AdjustedAt)),
            cancellationToken);

        var adjustedByIds = rows
            .Where(r => r.AdjustedBy.HasValue)
            .Select(r => r.AdjustedBy!.Value)
            .Distinct()
            .ToList();
        var userNames = new Dictionary<Guid, string>();
        foreach (var userId in adjustedByIds)
        {
            var account = await _users.FindAsync(userId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(account?.FullName))
                userNames[userId] = account.FullName;
        }

        var items = rows
            .Select(r => r.ToDto(r.AdjustedBy.HasValue ? userNames.GetValueOrDefault(r.AdjustedBy.Value) : null))
            .ToPagedList(page, pageSize, totalCount);

        return Result<PagedList<InventoryAdjustmentDto>>.Success(items);
    }

    private static IOrderedQueryable<TSource> SortDir<TSource, TKey>(
        IQueryable<TSource> source,
        System.Linq.Expressions.Expression<Func<TSource, TKey>> keySelector,
        string sortDir)
        => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector);
}
