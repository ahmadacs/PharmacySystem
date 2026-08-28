using Application.Common.Models;
using Application.Features.Dispensing.Dtos;
using Application.Features.Dispensing.Queries;
using Application.Features.Prescriptions.Dtos;
using Application.Features.Prescriptions.Queries;
using Domain.Entities.Dispensing;
using Domain.Entities.Prescriptions;

namespace Application.Common.Interfaces;

public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    Task<Prescription?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Prescription?> GetByIdWithItemsAndDoctorAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<PrescriptionListItemDto>> ListAsync(
        ListPrescriptionsQuery query,
        Guid? restrictedToDoctorId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DispensingRecordDto>> ListDispensingRecordsAsync(DispensingRecordListQuery query, CancellationToken cancellationToken = default);

    void AddDispensingRecord(DispensingRecord record);
}