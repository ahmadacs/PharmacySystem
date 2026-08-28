using Application.Common.Interfaces;
using Domain.Entities.Medicines;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class DeleteVariantCommandHandler : IRequestHandler<DeleteVariantCommand>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteVariantCommandHandler(IMedicineRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DeleteVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _repo.GetVariantByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(MedicineVariant), request.Id);

        _repo.RemoveVariant(variant);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}