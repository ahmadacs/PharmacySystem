using Application.Common.Models;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Features.Inventory.Queries;

public sealed record ListLowStockQuery : IRequest<Result<IReadOnlyList<LowStockDto>>>;