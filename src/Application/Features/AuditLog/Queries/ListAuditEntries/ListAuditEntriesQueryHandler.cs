using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed class ListAuditEntriesQueryHandler : IRequestHandler<ListAuditEntriesQuery, Result<PagedList<AuditEntryDto>>>
{
    private readonly IAuditRepository _audit;

    public ListAuditEntriesQueryHandler(IAuditRepository audit)
    {
        _audit = audit;
    }

    public async Task<Result<PagedList<AuditEntryDto>>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _audit.ListAsync(request, cancellationToken);
        return Result<PagedList<AuditEntryDto>>.Success(page);
    }
}