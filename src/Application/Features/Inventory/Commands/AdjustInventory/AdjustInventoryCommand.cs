using Application.Common.Caching;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record AdjustInventoryCommand(AdjustInventoryRequest Request) : IRequest<Guid>;