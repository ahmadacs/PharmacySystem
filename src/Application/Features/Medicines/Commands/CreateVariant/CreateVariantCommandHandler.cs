using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class CreateVariantCommandHandler : IRequestHandler<CreateVariantCommand, Result<Guid>>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateVariantCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var medicine = await _repo.GetByIdAsync(req.MedicineId, cancellationToken);
        if (medicine is null)
            return Result<Guid>.Failure($"Resource 'Medicine' with id '{req.MedicineId}' was not found.", 404);

        var existing = await _repo.FindVariantAsync(req.MedicineId, req.Form, req.Unit, req.Strength, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure(
                $"A variant '{req.Form} {req.Strength} {req.Unit}' already exists for this medicine.", 409);

        var variant = req.ToEntity();
        _repo.AddVariant(variant);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message, 422);
        }

        return Result<Guid>.Success(variant.Id);
    }
}