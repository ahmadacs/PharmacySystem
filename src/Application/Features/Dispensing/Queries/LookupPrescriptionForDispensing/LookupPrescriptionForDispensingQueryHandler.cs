using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Common;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Dispensing.Queries.LookupPrescriptionForDispensing;

public sealed class LookupPrescriptionForDispensingQueryHandler
    : IRequestHandler<LookupPrescriptionForDispensingQuery, Result<DispensingLookupResponse>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LookupPrescriptionForDispensingQueryHandler(
        IBaseRepository<Prescription> prescriptions,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _localizer = localizer;
    }

    public async Task<Result<DispensingLookupResponse>> Handle(
        LookupPrescriptionForDispensingQuery request,
        CancellationToken cancellationToken)
    {
        var shortCode = (request.ShortCode ?? string.Empty).Trim().ToUpperInvariant();
        if (shortCode.Length is < 6 or > 8)
            return Result<DispensingLookupResponse>.Failure(_localizer["InvalidShortCode"].Value, 400);

        var normalizedPhone = PhoneNumbers.NormalizeSaudiPhone(request.PhoneNumber);
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var row = await _prescriptions.GetReadAsync(
            LookupRowProjection(asOf),
            p => p.ShortCode == shortCode && p.Patient != null && p.Patient.PhoneNumber == normalizedPhone,
            cancellationToken);

        if (row is null)
            return Result<DispensingLookupResponse>.Failure(_localizer["PrescriptionLookupNotFound"].Value, 404);

        var items = row.Items
            .Select(i => i.ToDto())
            .OrderBy(i => i.PrescriptionItemId)
            .ToList();

        return Result<DispensingLookupResponse>.Success(row.ToResponse(items));
    }

    private static Expression<Func<Prescription, DispensingLookupRow>> LookupRowProjection(DateOnly asOf) => p => new DispensingLookupRow(
        p.Id,
        p.ShortCode,
        p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
        p.IssuedDate,
        p.Status,
        p.Items
            .OrderBy(i => i.Id)
            .Select(i => new DispensingLookupItemRow(
                i.Id,
                i.MedicineVariant != null && i.MedicineVariant.Medicine != null
                    ? i.MedicineVariant.Medicine.Name : "Unknown",
                i.MedicineVariant != null && i.MedicineVariant.Medicine != null
                    ? i.MedicineVariant.Medicine.NameAr : null,
                i.MedicineVariant != null
                    ? (Domain.Enums.MedicineForm?)i.MedicineVariant.Form : null,
                i.MedicineVariant != null
                    ? (Domain.Enums.MedicineUnit?)i.MedicineVariant.Unit : null,
                i.MedicineVariant != null ? (decimal?)i.MedicineVariant.Strength : null,
                i.PrescribedQuantity.Value,
                i.DispensedQuantity.Value,
                i.DosageInstructions,
                i.MedicineVariant != null
                    ? i.MedicineVariant.Batches.Where(b => b.ExpiryDate > asOf).Sum(b => (int?)b.QuantityAvailable.Value) ?? 0
                    : 0))
            .ToList());
}
