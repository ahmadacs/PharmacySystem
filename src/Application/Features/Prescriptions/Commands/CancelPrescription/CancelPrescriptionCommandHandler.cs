using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Commands;

public sealed class CancelPrescriptionCommandHandler : IRequestHandler<CancelPrescriptionCommand, Result>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CancelPrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IUnitOfWork uow,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(CancelPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.Id, cancellationToken);
        if (prescription is null)
            return Result.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        if (prescription.Status == PrescriptionStatus.Cancelled)
            return Result.Failure(_localizer["AlreadyCancelled"].Value, 409);
        if (prescription.Status == PrescriptionStatus.FullyDispensed)
            return Result.Failure(_localizer["CannotCancelDispensed"].Value, 409);

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