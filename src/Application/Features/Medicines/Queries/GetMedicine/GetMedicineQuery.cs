using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed record GetMedicineQuery(Guid Id) : IRequest<MedicineDetailsDto>;