using Application.Common.Caching;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record AddBatchCommand(AddBatchRequest Request, string? Reason = null) : IRequest<Result<Guid>>
{

    public const string DefaultCreationReason = "New batch added via the Medicines screen";
}