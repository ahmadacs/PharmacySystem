using Domain.Entities.Dispensing;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;

namespace Domain.Services;

public sealed class DispensingDomainService
{
    public DispensingRecord Dispense(
        Prescription prescription,
        IReadOnlyDictionary<Guid, MedicineVariant> variantsByVariantId,
        Guid pharmacistId,
        DateTime dispensedAt)
    {
        ArgumentNullException.ThrowIfNull(prescription);
        ArgumentNullException.ThrowIfNull(variantsByVariantId);

        var asOf = DateOnly.FromDateTime(dispensedAt);

        prescription.EnsureCanBeDispensed(asOf);

        var record = new DispensingRecord(prescription.Id, pharmacistId, dispensedAt);

        foreach (var item in prescription.Items.Where(i => !i.IsFullyDispensed))
        {
            if (!variantsByVariantId.TryGetValue(item.MedicineVariantId, out var variant))
                throw new MissingMedicineVariantException(
                    $"MedicineVariant '{item.MedicineVariantId}' for prescription item '{item.Id}' was not provided to the dispensing service.");

            var plan = variant.SelectBatchesForDispensing(item.RemainingQuantity.Value, asOf);

            foreach (var (batch, quantity) in plan)
            {
                batch.ReduceStock(quantity.Value, asOf);
                record.AddLine(item.Id, batch.Id, quantity.Value);
            }
        }

        prescription.ApplyDispensedQuantities(record.GetQuantitiesByPrescriptionItem());

        return record;
    }
}