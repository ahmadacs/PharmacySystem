using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Patients.Dtos;
using Domain.Entities.Prescriptions;
using Domain.Enums;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed class GetPatientPrescriptionsQueryHandler
    : IRequestHandler<GetPatientPrescriptionsQuery, Result<IReadOnlyList<PatientPrescriptionHistoryDto>>>
{
    private readonly IBaseRepository<Prescription> _prescriptions;

    public GetPatientPrescriptionsQueryHandler(IBaseRepository<Prescription> prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public async Task<Result<IReadOnlyList<PatientPrescriptionHistoryDto>>> Handle(
        GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var lookback = Math.Clamp(request.LookbackDays <= 0 ? 90 : request.LookbackDays, 30, 365);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(-lookback);

        var spec = new Specification<Prescription, PatientPrescriptionHistoryRow>(p => new PatientPrescriptionHistoryRow(
                    p.Id,
                    p.IssuedDate,
                    p.Status,
                    p.Items
                        .OrderBy(i => i.Id)
                        .Select(i => new PatientMedicationItemRow(
                            i.Id,
                            i.MedicineVariantId,
                            i.MedicineVariant!.MedicineId,
                            i.MedicineVariant!.Medicine != null ? i.MedicineVariant.Medicine.Name : "Unknown",
                            i.MedicineVariant!.Medicine != null ? i.MedicineVariant.Medicine.NameAr : null,
                            i.MedicineVariant!.Form,
                            i.MedicineVariant!.Unit,
                            i.MedicineVariant!.Strength,
                            i.DosageInstructions,
                            i.PrescribedQuantity.Value,
                            i.DispensedQuantity.Value,
                            i.IsRefillable,
                            i.RefillsAllowed,
                            i.RefillsUsed,
                            i.RefillIntervalDays,
                            i.LastDispensedAt))
                        .ToList()));
        spec.Where(p => p.PatientId == request.PatientId);
        spec.Where(p => p.Status != PrescriptionStatus.Cancelled && p.Status != PrescriptionStatus.Expired);
        spec.Where(p => p.IssuedDate >= cutoff);
        spec.Order(q => q.OrderByDescending(p => p.IssuedDate));

        var rows = await _prescriptions.ListAsync(spec, cancellationToken);

        return Result<IReadOnlyList<PatientPrescriptionHistoryDto>>.Success(
            rows.Select(r => r.ToDto(cutoff)).ToList());
    }
}
