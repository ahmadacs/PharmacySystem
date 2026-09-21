using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Inventory.Dtos;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class AddBatchCommandHandler : IRequestHandler<AddBatchCommand, Result<Guid>>
{
    private readonly IMedicineRepository _repo;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddBatchCommandHandler(IMedicineRepository repo, IAsyncQueryExecutor executor, IUnitOfWork uow,
        ICurrentUserService currentUser, NotificationOptions notificationOptions, IAttachmentUploadService attachments,
        IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _executor = executor;
        _uow = uow;
        _currentUser = currentUser;
        _notificationOptions = notificationOptions;
        _attachments = attachments;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(AddBatchCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var variantsForEvent = await _repo.GetForDispensingAsync([req.MedicineVariantId], cancellationToken);
        var variant = variantsForEvent.FirstOrDefault(v => v.Id == req.MedicineVariantId);
        if (variant is null)
        {
            // Fallback to single fetch if not found via batch method (e.g., no batches yet)
            variant = await _repo.GetVariantByIdAsync(req.MedicineVariantId, cancellationToken);
            if (variant is null)
                return Result<Guid>.Failure(_localizer["ResourceNotFound", "MedicineVariant", req.MedicineVariantId].Value, 404);
        }

        // Only the medicine name is needed to generate the batch number, so it
        // is projected here from the raw set — no Include-based finder.
        var medicineName = await _executor.SingleOrDefaultAsync(
            _repo.Query().Where(m => m.Id == variant.MedicineId).Select(m => m.Name),
            cancellationToken);
        if (medicineName is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "Medicine", variant.MedicineId].Value, 404);

        // Generate batch number: First 3 letters of medicine name + variant abbreviation + date
        var batchNumber = GenerateBatchNumber(medicineName, variant);

        if (await _repo.BatchNumberExistsAsync(batchNumber, null, cancellationToken))
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

        _repo.AddBatch(batch);
        _repo.AddAdjustment(adjustment);
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