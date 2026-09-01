using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Prescriptions.Commands;

public sealed class CancelPrescriptionCommandHandler : IRequestHandler<CancelPrescriptionCommand, Result>
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

    public async Task<Result> Handle(CancelPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.Id, cancellationToken);
        if (prescription is null)
            return Result.Failure($"Resource '{nameof(Prescription)}' with id '{request.Id}' was not found.", 404);

        try
        {
            prescription.Cancel();
        }
        catch (DomainException ex) when (ex is InvalidPrescriptionStatusException)
        {
            return Result.Failure(ex.Message, 409);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, 422);
        }

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex) when (ex is InvalidPrescriptionStatusException or RefillNotEligibleException)
        {
            return Result.Failure(ex.Message, 409);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, 422);
        }

        return Result.Success();
    }
}