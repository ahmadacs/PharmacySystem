using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Prescriptions.Dtos;
using Application.Resources;
using Domain.Entities.Prescriptions;
using Domain.Exceptions;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Prescriptions.Queries;

public sealed class GetPrescriptionQueryHandler : IRequestHandler<GetPrescriptionQuery, Result<PrescriptionDetailsDto>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IStaffService _staff;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetPrescriptionQueryHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IStaffService staff,
        IAsyncQueryExecutor executor,
        IStringLocalizer<SharedResource> localizer)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _staff = staff;
        _executor = executor;
        _localizer = localizer;
    }

    public async Task<Result<PrescriptionDetailsDto>> Handle(GetPrescriptionQuery request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (prescription is null)
            return Result<PrescriptionDetailsDto>.Failure(_localizer["ResourceNotFound", nameof(Prescription), request.Id].Value, 404);

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

        return Result<PrescriptionDetailsDto>.Success(prescription.ToDetailsDto(doctorName, infosById));
    }
}