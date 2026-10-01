using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Resources;
using Domain.Entities.Prescriptions;
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
        if (request.ItemIds is null || request.ItemIds.Count == 0)
            return Result.Failure(_localizer["RefillItemRequired"].Value, 400);

        var prescription = await _prescriptions.GetForRefillAsync(request.Id, cancellationToken);
        if (prescription is null)
            return Result.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        prescription.RegisterItemsRefill(request.ItemIds);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
