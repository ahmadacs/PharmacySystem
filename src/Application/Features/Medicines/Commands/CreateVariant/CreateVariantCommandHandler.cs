using Application.Common.Interfaces;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class CreateVariantCommandHandler : IRequestHandler<CreateVariantCommand, Guid>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateVariantCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var medicine = await _repo.GetByIdAsync(req.MedicineId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Medicine), req.MedicineId);

        var existing = await _repo.FindVariantAsync(req.MedicineId, req.Form, req.Unit, req.Strength, cancellationToken);
        if (existing is not null)
            throw new ConflictingOperationException(
                $"A variant '{req.Form} {req.Strength} {req.Unit}' already exists for this medicine.");

        var variant = req.ToEntity();
        _repo.AddVariant(variant);
        await _uow.SaveChangesAsync(cancellationToken);

        return variant.Id;
    }
}