using Domain.Entities.Dispensing;
using Domain.Entities.Prescriptions;

namespace Application.Common.Interfaces;

public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    Task<Prescription?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Prescription?> GetByIdWithItemsAndDoctorAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Prescriptions of one patient with patient + items loaded, newest first. Pure data access.</summary>
    Task<List<Prescription>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Dispensed totals per batch for the given batch ids. Pure data access.</summary>
    Task<Dictionary<Guid, int>> GetDispensedTotalsAsync(IReadOnlyCollection<Guid> batchIds, CancellationToken cancellationToken = default);

    /// <summary>Queryable dispensing-record set for handler-built searches. Pure data access.</summary>
    IQueryable<DispensingRecord> QueryDispensingRecords();

    /// <summary>Queryable dispensing-line set for handler-built subqueries. Pure data access.</summary>
    IQueryable<DispensingRecordItem> QueryDispensingRecordItems();

    void AddDispensingRecord(DispensingRecord record);
}