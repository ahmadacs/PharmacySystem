using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Medicines.Dtos;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Medicines.Commands;

public sealed class AddBatchCommandHandler : IRequestHandler<AddBatchCommand, Result<Guid>>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;

    public AddBatchCommandHandler(IMedicineRepository repo, IUnitOfWork uow,
        ICurrentUserService currentUser, NotificationOptions notificationOptions)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _notificationOptions = notificationOptions;
    }

    public async Task<Result<Guid>> Handle(AddBatchCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var variant = await _repo.GetVariantByIdAsync(req.MedicineVariantId, cancellationToken);
        if (variant is null)
            return Result<Guid>.Failure($"Resource 'MedicineVariant' with id '{req.MedicineVariantId}' was not found.", 404);

        // Get the parent medicine to generate batch number
        var medicineInfo = await _repo.GetByIdWithVariantsAsync(variant.MedicineId, cancellationToken);
        if (medicineInfo is null)
            return Result<Guid>.Failure($"Resource 'Medicine' with id '{variant.MedicineId}' was not found.", 404);

        // Generate batch number: First 3 letters of medicine name + variant abbreviation + date
        var batchNumber = GenerateBatchNumber(medicineInfo.Name, variant);

        if (await _repo.BatchNumberExistsAsync(batchNumber, null, cancellationToken))
            return Result<Guid>.Failure($"A batch with number '{batchNumber}' already exists.", 409);

        if (req.ExpiryDate <= req.ManufactureDate)
            return Result<Guid>.Failure("The expiry date must be after the manufacture date.", 409);

        // Packages are converted to base units via the variant's UnitOfMeasure
        // (e.g. 5 boxes of 30 tablets => 150 tablets), so stored quantities are
        // always whole multiples of UnitsPerPackage.
        var batch = req.ToEntity(variant.UnitOfMeasure, batchNumber);
        var totalUnits = batch.QuantityAvailable.Value;

        // Every batch creation is audited as a stock movement. The adjustment
        // type now comes from the caller (AddBatchRequest.AdjustmentType) so
        // the sign of QuantityChanged must match the type. Also include the
        // generated batch number in the adjustment for traceability.
        var adjustmentType = req.AdjustmentType;
        var isIncrease = adjustmentType is InventoryAdjustmentType.Increase
            or InventoryAdjustmentType.Returned or InventoryAdjustmentType.TransferIn;
        var quantityChanged = isIncrease ? totalUnits : -totalUnits;

        var adjustment = new InventoryAdjustment(
            batch.Id,
            adjustmentType,
            quantityChanged,
            request.Reason ?? AddBatchCommand.DefaultCreationReason,
            _currentUser.UserId,
            0,
            totalUnits,
            DateTime.UtcNow);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

        var medicineList = await _repo.GetMedicinesByVariantIdsForStockCheckAsync([variant.Id], cancellationToken);
        foreach (var medicineItem in medicineList)
            medicineItem.RaiseLowStockEventIfNeeded(asOf);

        _repo.AddBatch(batch);
        _repo.AddAdjustment(adjustment);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message, 422);
        }

        return Result<Guid>.Success(batch.Id);
    }

    private static string GenerateBatchNumber(string medicineName, MedicineVariant variant)
    {
        // First 3 letters of medicine name (uppercase, alphanumeric only)
        var namePart = new string(medicineName.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(namePart))
            namePart = "MED";

        // Variant abbreviation: Form (first letter) + Unit (first letter) + Strength
        var formPart = variant.Form.ToString()[0].ToString().ToUpperInvariant();
        var unitPart = variant.Unit.ToString()[0].ToString().ToUpperInvariant();
        var strengthPart = variant.Strength.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "");
        
        var variantPart = $"{formPart}{unitPart}{strengthPart}";
        if (string.IsNullOrWhiteSpace(variantPart))
            variantPart = "VAR";

        // Date part: YYMMDD
        var datePart = DateTime.UtcNow.ToString("yyMMdd");

        return $"{namePart}-{variantPart}-{datePart}";
    }
}