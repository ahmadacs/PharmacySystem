using Application.Common.Caching;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record CreateMedicineCommand(CreateMedicineRequest Request) : IRequest<Guid>;