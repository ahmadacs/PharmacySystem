using Application.Common.Models;
using Application.Features.AuditLog.Dtos;
using Application.Features.AuditLog.Queries;

namespace Application.Common.Interfaces;

public interface IAuditRepository
{
    Task<PagedResult<AuditEntryDto>> ListAsync(ListAuditEntriesQuery query, CancellationToken cancellationToken = default);
}