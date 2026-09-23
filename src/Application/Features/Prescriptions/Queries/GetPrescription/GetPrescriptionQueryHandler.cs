using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Prescriptions;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Queries;

public sealed class GetPrescriptionQueryHandler : IRequestHandler<GetPrescriptionQuery, Result<PrescriptionDetailsDto>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;
    private readonly IStaffService _staff;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetPrescriptionQueryHandler(
        IBaseRepository<Prescription> prescriptions,
        IStaffService staff,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _staff = staff;
        _localizer = localizer;
    }

    public async Task<Result<PrescriptionDetailsDto>> Handle(GetPrescriptionQuery request, CancellationToken cancellationToken)
    {
        // Single projection query: header + ordered items with variant/medicine
        // names inline through navigations (no Include — navigations inside a
        // Select need none). Same single round trip shape as the other details
        // screens; the old second variant-infos query is gone (2 queries -> 1).
        var spec = new Specification<Prescription, PrescriptionDetailsRow>(p => new PrescriptionDetailsRow(
            p.Id,
            p.DoctorId,
            p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName).Trim() : string.Empty,
            p.Patient != null ? p.Patient.DateOfBirth : default,
            p.Patient != null ? p.Patient.Age : 0,
            p.Patient != null ? p.Patient.PhoneNumber : null,
            p.Diagnosis,
            p.IssuedDate,
            p.Status.ToString(),
            p.CreatedBy,
            p.CreatedAt,
            p.Items
                .OrderBy(i => i.Id)
                .Select(i => new PrescriptionDetailsItemRow(
                    i.Id,
                    i.MedicineVariantId,
                    i.MedicineVariant != null && i.MedicineVariant.Medicine != null
                        ? i.MedicineVariant.Medicine.Name : "Unknown",
                    i.MedicineVariant != null
                        ? $"{i.MedicineVariant.Form} {i.MedicineVariant.Strength} {i.MedicineVariant.Unit}"
                        : string.Empty,
                    i.PrescribedQuantity.Value,
                    i.DispensedQuantity.Value,
                    i.DosageInstructions,
                    i.IsRefillable,
                    i.RefillsAllowed,
                    i.RefillsUsed,
                    i.RefillIntervalDays,
                    i.LastDispensedAt))
                .ToList()));
        spec.Where(p => p.Id == request.Id);

        var row = await _prescriptions.GetAsync(spec, cancellationToken);
        if (row is null)
            return Result<PrescriptionDetailsDto>.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        var doctorName = await _staff.GetDoctorNameAsync(row.DoctorId, cancellationToken) ?? string.Empty;

        return Result<PrescriptionDetailsDto>.Success(row.ToDetailsDto(doctorName));
    }
}
