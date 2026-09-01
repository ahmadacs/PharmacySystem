using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteBatchCommandHandler : IRequestHandler<DeleteBatchCommand, Result>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteBatchCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result> Handle(DeleteBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await _repo.GetBatchByIdAsync(request.Id, cancellationToken);
        if (batch is null)
            return Result.Failure($"Resource 'MedicineBatch' with id '{request.Id}' was not found.", 404);

        _repo.RemoveBatch(batch);
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