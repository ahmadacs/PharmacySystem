using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Resources;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Commands;

public sealed class CancelPrescriptionCommandHandler : IRequestHandler<CancelPrescriptionCommand, Result>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IUnitOfWork _uow;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CancelPrescriptionCommandHandler(
        IBaseRepository<Prescription> prescriptions,
        IUnitOfWork uow,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _uow = uow;
        _localizer = localizer;
    }

    public async Task<Result> Handle(CancelPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var byIdSpec = new Specification<Prescription, Prescription>(p => p).Tracked();
        byIdSpec.Where(p => p.Id == request.Id);
        var prescription = await _prescriptions.GetAsync(byIdSpec, cancellationToken);
        if (prescription is null)
            return Result.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        if (prescription.Status == PrescriptionStatus.Cancelled)
            return Result.Failure(_localizer["AlreadyCancelled"].Value, 409);
        if (prescription.Status == PrescriptionStatus.FullyDispensed)
            return Result.Failure(_localizer["CannotCancelDispensed"].Value, 409);

        prescription.Cancel();

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}