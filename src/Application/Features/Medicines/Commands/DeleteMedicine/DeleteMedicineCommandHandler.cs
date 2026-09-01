using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteMedicineCommandHandler : IRequestHandler<DeleteMedicineCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (medicine is null)
            return Result.Failure($"Resource 'Medicine' with id '{request.Id}' was not found.", 404);

        _repo.Remove(medicine);
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