using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Application.Common.Specifications;
using Application.Features.Files.Common;
using Application.Features.Inventory.Dtos;
using Application.Resources;
using Domain.Entities.Inventory;
using Domain.Entities.Medicines;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Inventory.Commands;

public sealed class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, Result<Guid>>
{
    private readonly IBaseRepository<MedicineVariant> _variants;
    private readonly IBaseRepository<Medicine> _medicines;
    private readonly IBaseRepository<MedicineBatch> _batches;
    private readonly IBaseRepository<InventoryAdjustment> _adjustments;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AdjustInventoryCommandHandler(IBaseRepository<MedicineVariant> variants, IBaseRepository<Medicine> medicines, IBaseRepository<MedicineBatch> batches,
        IBaseRepository<InventoryAdjustment> adjustments, IUnitOfWork uow,
        ICurrentUserService currentUser, NotificationOptions notificationOptions, IAttachmentUploadService attachments,
        IStringLocalizer<SharedResource> localizer)
    {
        _variants = variants;
        _medicines = medicines;
        _batches = batches;
        _adjustments = adjustments;
        _uow = uow;
        _currentUser = currentUser;
        _notificationOptions = notificationOptions;
        _attachments = attachments;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        // Tracked: AdjustQuantity below must persist on SaveChanges.
        var batchSpec = new Specification<MedicineBatch, MedicineBatch>(b => b).Tracked();
        batchSpec.Where(b => b.Id == req.MedicineBatchId);
        var batch = await _batches.GetAsync(batchSpec, cancellationToken);
        if (batch is null)
            return Result<Guid>.Failure(_localizer["ResourceNotFound", "MedicineBatch", req.MedicineBatchId].Value, 404);

        var quantityBefore = batch.QuantityAvailable.Value;
        var delta = req.Type is InventoryAdjustmentType.Increase
                or InventoryAdjustmentType.Returned
                or InventoryAdjustmentType.TransferIn
            ? req.Quantity
            : -req.Quantity;
        var adjustment = req.ToEntity(batch.Id, _currentUser.UserId, quantityBefore, quantityBefore + delta);
        batch.AdjustQuantity(adjustment.QuantityChanged);

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        batch.RaiseNearExpiryEventIfNeeded(asOf, _notificationOptions.ExpiryWarningDays);

        // Tracked variant with batches + medicine for the low-stock domain
        // event (reads variant.Batches and variant.Medicine?.Name), assembled
        // by EF relationship fix-up (no Include).
        var eventVariantSpec = new Specification<MedicineVariant, MedicineVariant>(v => v).Tracked();
        eventVariantSpec.Where(v => v.Id == batch.MedicineVariantId);
        var eventVariant = await _variants.GetAsync(eventVariantSpec, cancellationToken);

        if (eventVariant is not null)
        {
            var eventBatchesSpec = new Specification<MedicineBatch, MedicineBatch>(b => b).Tracked();
            eventBatchesSpec.Where(b => b.MedicineVariantId == batch.MedicineVariantId);
            await _batches.ListAsync(eventBatchesSpec, cancellationToken);

            var eventMedicineSpec = new Specification<Medicine, Medicine>(m => m).Tracked();
            eventMedicineSpec.Where(m => m.Id == eventVariant.MedicineId);
            await _medicines.GetAsync(eventMedicineSpec, cancellationToken);

            eventVariant.RaiseLowStockEventIfNeeded(asOf);
        }

        _adjustments.Add(adjustment);
        await _uow.SaveChangesAsync(cancellationToken);

        await _attachments.UploadAsync("InventoryAdjustment", adjustment.Id, req.File, cancellationToken);

        return Result<Guid>.Success(adjustment.Id);
    }
}
