using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteVariantCommandHandler : IRequestHandler<DeleteVariantCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteVariantCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _repo.GetVariantByIdAsync(request.Id, cancellationToken);
        if (variant is null)
            return Result.Failure($"Resource 'MedicineVariant' with id '{request.Id}' was not found.", 404);

        _repo.RemoveVariant(variant);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, 422);
        }

        return Result.Success();
    }
}