using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Common.Specifications;
using Application.Features.Inventory.Dtos;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class AddBatchCommandHandler : IRequestHandler<AddBatchCommand, Result<Guid>>
{
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<MedicineVariant> _variants;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IBaseRepository<InventoryAdjustment> _adjustments;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddBatchCommandHandler(IBaseRepository<Medicine> medicines, IBaseRepository<MedicineVariant> variants,
        IBaseRepository<MedicineBatch> batches, IBaseRepository<InventoryAdjustment> adjustments, IUnitOfWork uow,
        ICurrentUserService currentUser, NotificationOptions notificationOptions, IAttachmentUploadService attachments,
        IStringLocalizer<SharedResource> localizer)
    {
        _medicines = medicines;
        _variants = variants;
        _batches = batches;
        _adjustments = adjustments;
        _uow = uow;
        _currentUser = currentUser;
        _notificationOptions = notificationOptions;
        _attachments = attachments;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(AddBatchCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        // Tracked loads assembled by EF relationship fix-up (no Include):
        // the variant first, then its batches and medicine; fix-up populates
        // variant.Batches / variant.Medicine in memory. The low-stock event
        // below reads both, so all three loads are tracked.
        var variantSpec = new Specification<MedicineVariant, MedicineVariant>(v => v).Tracked();
        variantSpec.Where(v => v.Id == req.MedicineVariantId);
        var variant = await _variants.GetAsync(variantSpec, cancellationToken);
        if (variant is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "MedicineVariant", req.MedicineVariantId].Value, 404);

        var variantBatchesSpec = new Specification<MedicineBatch, MedicineBatch>(b => b).Tracked();
        variantBatchesSpec.Where(b => b.MedicineVariantId == req.MedicineVariantId);
        await _batches.ListAsync(variantBatchesSpec, cancellationToken);

        var medicineSpec = new Specification<Medicine, Medicine>(m => m).Tracked();
        medicineSpec.Where(m => m.Id == variant.MedicineId);
        var medicine = await _medicines.GetAsync(medicineSpec, cancellationToken);
        if (medicine is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "Medicine", variant.MedicineId].Value, 404);

        // Generate batch number: First 3 letters of medicine name + variant abbreviation + date
        var batchNumber = GenerateBatchNumber(medicine.Name, variant);

        var batchNumberSpec = new Specification<MedicineBatch, MedicineBatch>(b => b);
        batchNumberSpec.Where(b => b.BatchNumber == batchNumber.Trim());
        if (await _batches.CountAsync(batchNumberSpec, cancellationToken) > 0)
            return Result<Guid>.Failure(_localizer["BatchNumberExists", batchNumber].Value, 409);

        if (req.ExpiryDate <= req.ManufactureDate)
            return Result<Guid>.Failure(_localizer["ExpiryAfterManufacture"].Value, 422);

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

        var adjustment = req.ToEntity(
            batch.Id,
            quantityChanged,
            _currentUser.UserId,
            0,
            totalUnits,
            request.Reason ?? AddBatchCommand.DefaultCreationReason);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

        // Evaluate low-stock for the variant AFTER the new batch is added.
        variant.RaiseLowStockEventIfNeededWithAdditional(asOf, totalUnits);

        _batches.Add(batch);
        _adjustments.Add(adjustment);
        await _uow.SaveChangesAsync(cancellationToken);

        await _attachments.UploadAsync("Batch", batch.Id, req.File, cancellationToken);

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