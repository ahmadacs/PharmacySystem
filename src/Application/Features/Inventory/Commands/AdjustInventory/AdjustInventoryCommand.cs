using Application.Common.Caching;
using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record AdjustInventoryCommand(AdjustInventoryRequest Request) : IRequest<Result<Guid>>;