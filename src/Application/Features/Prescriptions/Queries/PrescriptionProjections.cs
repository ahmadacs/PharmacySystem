using System.Linq.Expressions;
using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Prescriptions;

namespace Application.Features.Prescriptions.Queries;

internal static class PrescriptionProjections
{
    public static readonly Expression<Func<Prescription, PrescriptionDetailsRow>> ToDetailsRow = p => new PrescriptionDetailsRow(
        p.Id,
        p.ShortCode,
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
                i.MedicineVariant != null && i.MedicineVariant.Medicine != null
                    ? i.MedicineVariant.Medicine.NameAr : null,
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
            .ToList());

    public static readonly Expression<Func<Prescription, PrescriptionListRow>> ToListRow = p => new PrescriptionListRow(
        p.Id,
        p.ShortCode,
        p.DoctorId,
        p.Patient != null ? (p.Patient.FirstName + " " + p.Patient.LastName) : string.Empty,
        p.Patient != null ? p.Patient.DateOfBirth : default,
        p.Patient != null ? p.Patient.PhoneNumber : null,
        p.IssuedDate,
        p.Status,
        p.Items.Count(),
        p.CreatedAt);
}
