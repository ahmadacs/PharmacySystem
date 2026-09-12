using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed class ListAuditEntriesQueryHandler : IRequestHandler<ListAuditEntriesQuery, Result<PagedList<AuditEntryDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAuditRepository _audit;
    private readonly IUserManager _users;

    public ListAuditEntriesQueryHandler(IAuditRepository audit, IUserManager users)
    {
        _audit = audit;
        _users = users;
    }

    public async Task<Result<PagedList<AuditEntryDto>>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _audit.ListAsync(
            request,
            request.Action,
            request.Entity,
            request.From,
            request.To,
            cancellationToken);

        var authorIds = page.Items
            .Where(e => e.ChangedBy.HasValue)
            .Select(e => e.ChangedBy!.Value)
            .Distinct()
            .ToList();
        var authorNames = await _users.GetDisplayNamesAsync(authorIds, cancellationToken);

        var items = page.Items
            .Select(r => r.ToDto(
                r.ChangedBy.HasValue ? authorNames.GetValueOrDefault(r.ChangedBy.Value) : null,
                DeserializeChanges(r.ChangesJson)))
            .ToPagedList(page.Page, page.PageSize, page.TotalCount);

        return Result<PagedList<AuditEntryDto>>.Success(items);
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
