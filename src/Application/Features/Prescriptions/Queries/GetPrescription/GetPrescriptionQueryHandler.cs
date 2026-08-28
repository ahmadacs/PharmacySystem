using Application.Common.Interfaces;
using Application.Features.Prescriptions.Dtos;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using Domain.Enums;
using MediatR;

namespace Application.Features.Prescriptions.Queries;

public sealed class GetPrescriptionQueryHandler : IRequestHandler<GetPrescriptionQuery, PrescriptionDetailsDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IStaffService _staff;
    private readonly IAsyncQueryExecutor _executor;

    public GetPrescriptionQueryHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IStaffService staff,
        IAsyncQueryExecutor executor)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _staff = staff;
        _executor = executor;
    }

    public async Task<PrescriptionDetailsDto> Handle(GetPrescriptionQuery request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdWithItemsAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Prescription), request.Id);

        var doctorName = await _staff.GetDoctorNameAsync(prescription.DoctorId, cancellationToken) ?? string.Empty;

        var variantIds = prescription.Items.Select(i => i.MedicineVariantId).Distinct().ToList();
        var infos = await _executor.ToListAsync(
            _medicines.Query()
                .SelectMany(m => m.Variants
                    .Where(v => variantIds.Contains(v.Id))
                    .Select(v => new { VariantId = v.Id, MedicineName = m.Name, v.Form, v.Unit, v.Strength })),
            cancellationToken);
        var infosById = infos.ToDictionary(
            n => n.VariantId,
            n => new VariantInfo(
                n.MedicineName,
                $"{n.Form} {n.Strength} {n.Unit}"));

        return prescription.ToDetailsDto(doctorName, infosById);
    }
}