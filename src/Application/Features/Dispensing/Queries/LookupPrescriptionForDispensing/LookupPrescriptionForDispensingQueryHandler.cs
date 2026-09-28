using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Common;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Dispensing.Queries.LookupPrescriptionForDispensing;

public sealed class LookupPrescriptionForDispensingQueryHandler
    : IRequestHandler<LookupPrescriptionForDispensingQuery, Result<DispensingLookupResponse>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LookupPrescriptionForDispensingQueryHandler(
        IBaseRepository<Prescription> prescriptions,
        IBaseRepository<MedicineBatch> batches,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _batches = batches;
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

        var selector = (Expression<Func<Prescription, DispensingLookupRow>>)(p => new DispensingLookupRow(
            p.Id,
            p.ShortCode,
            p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
            p.Patient != null ? p.Patient.PhoneNumber : null,
            p.IssuedDate,
            p.Status,
            p.Items
                .OrderBy(i => i.Id)
                .Select(i => new DispensingLookupItemRow(
                    i.Id,
                    i.MedicineVariantId,
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
                    i.DosageInstructions))
                .ToList()));

        var row = await _prescriptions.GetAsync(selector, p => p.ShortCode == shortCode, cancellationToken);

        if (row is null || !string.Equals(row.PatientPhoneNumber, normalizedPhone, StringComparison.Ordinal))
            return Result<DispensingLookupResponse>.Failure(_localizer["PrescriptionLookupNotFound"].Value, 404);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var variantIds = row.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
        var stockRows = await _batches.ListAsync(
            b => new BatchStockRow(b.MedicineVariantId, b.QuantityAvailable.Value, b.ExpiryDate),
            b => variantIds.Contains(b.MedicineVariantId),
            cancellationToken);
        var availableByVariant = stockRows
            .Where(s => s.ExpiryDate > asOf)
            .GroupBy(s => s.MedicineVariantId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.QuantityAvailable));

        var items = row.Items
            .Select(i => new DispensingLookupItemDto(
                i.PrescriptionItemId,
                i.MedicineName,
                i.MedicineNameAr,
                PrescriptionMapping.BuildVariantName(i.Form, i.Strength, i.Unit),
                (int?)i.Form,
                (int?)i.Unit,
                i.Strength,
                i.DosageInstructions,
                i.PrescribedQuantity,
                i.DispensedQuantity,
                i.PrescribedQuantity - i.DispensedQuantity,
                availableByVariant.GetValueOrDefault(i.MedicineVariantId)))
            .OrderBy(i => i.PrescriptionItemId)
            .ToList();

        return Result<DispensingLookupResponse>.Success(new DispensingLookupResponse(
            row.PrescriptionId,
            row.ShortCode,
            row.PatientName,
            row.IssuedDate,
            row.Status.ToString(),
            items));
    }

    private sealed record DispensingLookupItemRow(
        Guid PrescriptionItemId,
        Guid MedicineVariantId,
        string MedicineName,
        string? MedicineNameAr,
        Domain.Enums.MedicineForm? Form,
        Domain.Enums.MedicineUnit? Unit,
        decimal? Strength,
        int PrescribedQuantity,
        int DispensedQuantity,
        string? DosageInstructions);

    private sealed record BatchStockRow(Guid MedicineVariantId, int QuantityAvailable, DateOnly ExpiryDate);

    private sealed record DispensingLookupRow(
        Guid PrescriptionId,
        string ShortCode,
        string PatientName,
        string? PatientPhoneNumber,
        DateOnly IssuedDate,
        Domain.Enums.PrescriptionStatus Status,
        IReadOnlyList<DispensingLookupItemRow> Items);
}
