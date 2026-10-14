using Domain.Entities.Dispensing;
using Domain.Entities.Medicines;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;

namespace Domain.Services;

public sealed class DispensingDomainService
{
    public DispensingRecord Dispense(
        Prescription prescription,
        IReadOnlyDictionary<Guid, MedicineVariant> variantsById,
        Guid pharmacistId,
        DateTime dispensedAt)
    {
        ArgumentNullException.ThrowIfNull(prescription);
        ArgumentNullException.ThrowIfNull(variantsById);

        var asOf = DateOnly.FromDateTime(dispensedAt);

        prescription.EnsureCanBeDispensed(asOf);

        // Snapshot pending items once (non-empty is guaranteed by EnsureCanBeDispensed):
        // validate everything before mutating any batch.
        var pending = prescription.Items.Where(i => !i.IsFullyDispensed).ToList();

        foreach (var item in pending)
            item.EnsureRefillIntervalSatisfied(asOf);

        var record = new DispensingRecord(prescription.Id, pharmacistId, dispensedAt);

        foreach (var item in pending)
        {
            if (!variantsById.TryGetValue(item.MedicineVariantId, out var variant))
                throw new MissingMedicineVariantException(item.MedicineVariantId, item.Id);

            var plan = variant.SelectBatchesForDispensing(item.RemainingQuantity.Value, asOf);

            foreach (var (batch, quantity) in plan)
            {
                batch.ReduceStock(quantity.Value, asOf);
                record.AddLine(item.Id, batch.Id, quantity.Value);
            }
        }

        prescription.ApplyDispensedQuantities(record.GetQuantitiesByPrescriptionItem(), asOf);

        return record;
    }
}