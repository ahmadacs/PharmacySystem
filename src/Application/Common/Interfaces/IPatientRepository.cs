using Domain.Entities.Patients;

namespace Application.Common.Interfaces;

public interface IPatientRepository : IBaseRepository<Patient>
{
    Task<Patient?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
}