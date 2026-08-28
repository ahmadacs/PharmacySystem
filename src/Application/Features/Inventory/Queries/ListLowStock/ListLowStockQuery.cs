using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed record ListLowStockQuery : IRequest<IReadOnlyList<LowStockDto>>;