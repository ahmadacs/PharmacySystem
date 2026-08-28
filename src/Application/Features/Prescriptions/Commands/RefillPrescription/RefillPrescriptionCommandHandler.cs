using Application.Common.Interfaces;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed class RefillPrescriptionCommandHandler : IRequestHandler<RefillPrescriptionCommand>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IUnitOfWork _uow;

    public RefillPrescriptionCommandHandler(IPrescriptionRepository prescriptions, IUnitOfWork uow)
    {
        _prescriptions = prescriptions;
        _uow = uow;
    }

    public async Task Handle(RefillPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdWithItemsAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Prescription), request.Id);

        prescription.RegisterRefill();
        await _uow.SaveChangesAsync(cancellationToken);
    }
}