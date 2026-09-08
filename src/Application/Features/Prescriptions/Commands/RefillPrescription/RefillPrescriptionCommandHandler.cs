using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Commands;

public sealed class RefillPrescriptionCommandHandler : IRequestHandler<RefillPrescriptionCommand, Result>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RefillPrescriptionCommandHandler(IPrescriptionRepository prescriptions, IUnitOfWork uow, IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(RefillPrescriptionCommand request, CancellationToken cancellationToken)
    {
        if (request.ItemIds.Count == 0)
            return Result.Failure(_localizer["RefillItemRequired"].Value, 400);

        var prescription = await _prescriptions.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (prescription is null)
            return Result.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        // Localized pre-checks mirror the domain rules: the domain still
        // re-validates as a safety net (English fallback, unreachable here).
        if (prescription.Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Expired)
            return Result.Failure(_localizer["RefillPrescriptionStatus", request.Id, prescription.Status].Value, 409);

        foreach (var itemId in request.ItemIds.Distinct())
        {
            var item = prescription.Items.SingleOrDefault(i => i.Id == itemId);
            if (item is null)
                return Result.Failure(_localizer["RefillItemNotInPrescription", itemId, request.Id].Value, 409);
            if (!item.IsRefillable)
                return Result.Failure(_localizer["RefillItemNotRefillable", itemId].Value, 409);
            if (!item.IsFullyDispensed)
                return Result.Failure(_localizer["RefillItemNotDispensed", itemId].Value, 409);
            if (item.RefillsUsed >= item.RefillsAllowed)
                return Result.Failure(_localizer["RefillItemExhausted", itemId, item.RefillsUsed, item.RefillsAllowed].Value, 409);
        }

        try
        {
            prescription.RegisterItemsRefill(request.ItemIds);
        }
        catch (DomainException ex) when (ex is InvalidPrescriptionStatusException or RefillNotEligibleException)
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
