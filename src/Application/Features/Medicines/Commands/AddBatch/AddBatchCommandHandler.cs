using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Files.Common;
using Application.Features.Inventory.Dtos;
using Application.Features.Medicines.Dtos;
using Application.Resources;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Medicines.Commands;

public sealed class AddBatchCommandHandler : IRequestHandler<AddBatchCommand, Result<Guid>>
{
    private readonly IMedicineVariantRepository _variants;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IBaseRepository<InventoryAdjustment> _adjustments;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AddBatchCommandHandler(IMedicineVariantRepository variants,
        IBaseRepository<MedicineBatch> batches, IBaseRepository<InventoryAdjustment> adjustments, IUnitOfWork uow,
        ICurrentUserService currentUser, NotificationOptions notificationOptions, IAttachmentUploadService attachments,
        IStringLocalizer<SharedResource> localizer)
    {
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
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var variant = await _variants.GetForAddBatchAsync(req.MedicineVariantId, cancellationToken);

        if (variant is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "MedicineVariant", req.MedicineVariantId].Value, 404);

        var medicineName = variant.Medicine?.Name;
        if (medicineName is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "Medicine", variant.MedicineId].Value, 404);

        var batchNumber = MedicineBatch.GenerateNumber(
            medicineName, variant.Form, variant.Unit, variant.Strength, asOf);

        if (await _batches.ExistsAsync(b => b.BatchNumber == batchNumber.Trim(), cancellationToken))
            return Result<Guid>.Failure(_localizer["BatchNumberExists", batchNumber].Value, 409);

        var batch = req.ToEntity(variant.UnitOfMeasure, batchNumber);
        var totalUnits = batch.QuantityAvailable.Value;

        var quantityChanged = InventoryAdjustment.SignedQuantity(req.AdjustmentType, totalUnits);

        var adjustment = req.ToEntity(
            batch.Id,
            quantityChanged,
            _currentUser.UserId,
            0,
            totalUnits,
            request.Reason ?? AddBatchCommand.DefaultCreationReason);

        batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

        variant.RaiseLowStockEventIfNeededWithAdditional(asOf, totalUnits);

        _batches.Add(batch);
        _adjustments.Add(adjustment);
        await _uow.SaveChangesAsync(cancellationToken);

        await _attachments.UploadAsync("Batch", batch.Id, req.File, cancellationToken);

        return Result<Guid>.Success(batch.Id);
    }
}