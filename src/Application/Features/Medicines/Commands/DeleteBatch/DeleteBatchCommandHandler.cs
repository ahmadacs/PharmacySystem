using Application.Common.Interfaces;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteBatchCommandHandler : IRequestHandler<DeleteBatchCommand>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteBatchCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DeleteBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await _repo.GetBatchByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(MedicineBatch), request.Id);

        _repo.RemoveBatch(batch);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}