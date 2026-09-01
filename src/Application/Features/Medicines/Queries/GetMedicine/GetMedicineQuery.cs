using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using MediatR;

namespace Application.Features.Medicines.Queries;

public sealed record GetMedicineQuery(Guid Id) : IRequest<Result<MedicineDetailsDto>>;