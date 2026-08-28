using Application.Common.Caching;
using MediatR;

namespace Application.Features.Medicines.Commands;

[InvalidateCache(CacheTags.Medicines, CacheTags.Inventory)]
public sealed record DeleteMedicineCommand(Guid Id) : IRequest;