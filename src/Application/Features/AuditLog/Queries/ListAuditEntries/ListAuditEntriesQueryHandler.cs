using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using MediatR;

namespace Application.Features.AuditLog.Queries;

public sealed class ListAuditEntriesQueryHandler : IRequestHandler<ListAuditEntriesQuery, PagedResult<AuditEntryDto>>
{
    private readonly IAuditRepository _audit;

    public ListAuditEntriesQueryHandler(IAuditRepository audit)
    {
        _audit = audit;
    }

    public Task<PagedResult<AuditEntryDto>> Handle(
        ListAuditEntriesQuery request,
        CancellationToken cancellationToken)
        => _audit.ListAsync(request, cancellationToken);
}