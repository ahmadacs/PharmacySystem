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
    private readonly IMedicineRepository _medicines;
    private readonly IAsyncQueryExecutor _executor;

    public GetPatientPrescriptionsQueryHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IAsyncQueryExecutor executor)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<PatientPrescriptionHistoryDto>>> Handle(
        GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        // Clamp to a sane medical window: 30..365 days, default 180
        // (two chronic cycles of max 90-day refill interval).
        var lookback = Math.Clamp(request.LookbackDays <= 0 ? 180 : request.LookbackDays, 30, 365);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(-lookback);

        var all = await _prescriptions.GetByPatientIdAsync(request.PatientId, cancellationToken);

        var inWindow = all
            .Where(p => p.Status is not PrescriptionStatus.Cancelled and not PrescriptionStatus.Expired)
            .Where(p => p.IssuedDate >= cutoff)
            .OrderByDescending(p => p.IssuedDate)
            .ToList();

        if (inWindow.Count == 0)
            return Result<IReadOnlyList<PatientPrescriptionHistoryDto>>.Success([]);

        var variantIds = inWindow
            .SelectMany(p => p.Items)
            .Select(i => i.MedicineVariantId)
            .Distinct()
            .ToList();

        var infos = await _executor.ToListAsync(
            _medicines.Query()
                .SelectMany(m => m.Variants
                    .Where(v => variantIds.Contains(v.Id))
                    .Select(v => new
                    {
                        VariantId = v.Id,
                        MedicineId = m.Id,
                        MedicineName = m.Name,
                        MedicineNameAr = m.NameAr,
                        v.Form,
                        v.Unit,
                        v.Strength
                    })),
            cancellationToken);

        var infosById = infos.ToDictionary(
            n => n.VariantId,
            n => n);

        var result = inWindow.Select(p =>
        {
            var status = p.Status.ToString();
            var items = p.Items
                .OrderBy(i => i.Id)
                .Select(i =>
                {
                    infosById.TryGetValue(i.MedicineVariantId, out var info);
                    var active = PatientMedicationMapping.IsActive(
                        status,
                        i.IsRefillable,
                        i.RefillsUsed,
                        i.RefillsAllowed,
                        i.LastDispensedAt,
                        p.IssuedDate,
                        cutoff);
                    return new PatientMedicationItemDto(
                        i.Id,
                        i.MedicineVariantId,
                        info?.MedicineId ?? Guid.Empty,
                        info?.MedicineName ?? "Unknown",
                        info?.MedicineNameAr,
                        info is null ? string.Empty : $"{info.Form} {info.Strength} {info.Unit}",
                        info?.Form.ToString() ?? string.Empty,
                        info?.Unit.ToString() ?? string.Empty,
                        info?.Strength ?? 0,
                        i.DosageInstructions,
                        i.PrescribedQuantity.Value,
                        i.DispensedQuantity.Value,
                        i.RemainingQuantity.Value,
                        i.IsRefillable,
                        i.RefillsAllowed,
                        i.RefillsUsed,
                        i.RefillIntervalDays,
                        i.LastDispensedAt,
                        PatientMedicationMapping.NextEligible(i.LastDispensedAt, i.RefillIntervalDays),
                        active);
                })
                .ToList();

            return new PatientPrescriptionHistoryDto(
                p.Id,
                p.IssuedDate,
                status,
                p.Items.Count,
                items);
        }).ToList();

        return Result<IReadOnlyList<PatientPrescriptionHistoryDto>>.Success(result);
    }
}
