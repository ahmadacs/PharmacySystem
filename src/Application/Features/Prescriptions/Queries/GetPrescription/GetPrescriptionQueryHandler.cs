using System.Linq.Expressions;
using Application.Common.Interfaces;
using Application.Common.Models;
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
        // Select need none). Patient name/info are scalar columns; age/status/
        // variant display strings are derived in PrescriptionMapping (not SQL).
        var selector = (Expression<Func<Prescription, PrescriptionDetailsRow>>)(p => new PrescriptionDetailsRow(
            p.Id,
            p.DoctorId,
            p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
            p.Patient != null ? p.Patient.DateOfBirth : default,
            p.Patient != null ? p.Patient.PhoneNumber : null,
            p.Diagnosis,
            p.IssuedDate,
            p.Status,
            p.CreatedBy,
            p.CreatedAt,
            p.Items
                .OrderBy(i => i.Id)
                .Select(i => new PrescriptionDetailsItemRow(
                    i.Id,
                    i.MedicineVariantId,
                    i.MedicineVariant != null && i.MedicineVariant.Medicine != null
                        ? i.MedicineVariant.Medicine.Name : "Unknown",
                    i.MedicineVariant != null ? (Domain.Enums.MedicineForm?)i.MedicineVariant.Form : null,
                    i.MedicineVariant != null ? (Domain.Enums.MedicineUnit?)i.MedicineVariant.Unit : null,
                    i.MedicineVariant != null ? (decimal?)i.MedicineVariant.Strength : null,
                    i.PrescribedQuantity.Value,
                    i.DispensedQuantity.Value,
                    i.DosageInstructions,
                    i.IsRefillable,
                    i.RefillsAllowed,
                    i.RefillsUsed,
                    i.RefillIntervalDays,
                    i.LastDispensedAt))
                .ToList()));

        var row = await _prescriptions.GetAsync(selector, p => p.Id == request.Id, cancellationToken);
        if (row is null)
            return Result<PrescriptionDetailsDto>.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

        var doctorName = await _staff.GetDoctorNameAsync(row.DoctorId, cancellationToken) ?? string.Empty;

        return Result<PrescriptionDetailsDto>.Success(row.ToDetailsDto(doctorName));
    }
}
