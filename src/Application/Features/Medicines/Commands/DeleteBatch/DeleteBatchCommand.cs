using Application.Common.Caching;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Medicines.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record DeleteBatchCommand(Guid Id) : IRequest<Result>;