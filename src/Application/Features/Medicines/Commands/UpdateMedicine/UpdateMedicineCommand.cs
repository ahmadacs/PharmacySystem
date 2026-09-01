using Application.Common.Caching;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record UpdateMedicineCommand(UpdateMedicineRequest Request) : IRequest<Result>;