using Application.Common.Interfaces;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed class CancelPrescriptionCommandHandler : IRequestHandler<CancelPrescriptionCommand>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IUnitOfWork _uow;

    public CancelPrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IUnitOfWork uow)
    {
        _prescriptions = prescriptions;
        _uow = uow;
    }

    public async Task Handle(CancelPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Prescription), request.Id);

        prescription.Cancel();
        await _uow.SaveChangesAsync(cancellationToken);
    }
}