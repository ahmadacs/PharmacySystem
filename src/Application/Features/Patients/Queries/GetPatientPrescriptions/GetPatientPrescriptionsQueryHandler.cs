using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Patients.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Features.Patients.Queries.GetPatientPrescriptions;

public sealed class GetPatientPrescriptionsQueryHandler
    : IRequestHandler<GetPatientPrescriptionsQuery, Result<IReadOnlyList<PatientPrescriptionHistoryDto>>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IAsyncQueryExecutor _executor;

    public GetPatientPrescriptionsQueryHandler(
        IPrescriptionRepository prescriptions,
        IAsyncQueryExecutor executor)
    {
        _prescriptions = prescriptions;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<PatientPrescriptionHistoryDto>>> Handle(
        GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var lookback = Math.Clamp(request.LookbackDays <= 0 ? 180 : request.LookbackDays, 30, 365);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(-lookback);

        var rows = await _executor.ToListAsync(
            _prescriptions.Query()
                .Where(p => p.PatientId == request.PatientId)
                .Where(p => p.Status != PrescriptionStatus.Cancelled && p.Status != PrescriptionStatus.Expired)
                .Where(p => p.IssuedDate >= cutoff)
                .OrderByDescending(p => p.IssuedDate)
                .Select(p => new PatientPrescriptionHistoryRow(
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
                        .ToList())),
            cancellationToken);

        return Result<IReadOnlyList<PatientPrescriptionHistoryDto>>.Success(
            rows.Select(r => r.ToDto(cutoff)).ToList());
    }
}
