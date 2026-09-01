using Application.Common.Caching;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using MediatR;

namespace Application.Features.Dispensing.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record DispensePrescriptionCommand(DispensePrescriptionRequest Request) : IRequest<Result<Guid>>;