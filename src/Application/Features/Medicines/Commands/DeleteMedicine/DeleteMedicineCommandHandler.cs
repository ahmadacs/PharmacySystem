using Application.Common.Interfaces;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteMedicineCommandHandler : IRequestHandler<DeleteMedicineCommand>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteMedicineCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DeleteMedicineCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Medicine), request.Id);

        _repo.Remove(medicine);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}