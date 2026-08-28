using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Patients;

namespace Application.Common.Interfaces;

public interface IPatientRepository : IBaseRepository<Patient>
{
    Task<Patient?> FindByNameAsync(string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<Patient?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Patient> GetOrCreateAsync(string firstName, string lastName, DateOnly dateOfBirth, string phoneNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionListItemDto>> GetPrescriptionsAsync(Guid patientId, CancellationToken cancellationToken = default);
}