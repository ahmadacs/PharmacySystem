using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
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
    private readonly IMedicineVariantRepository _variants;
    private readonly IRepository<MedicineBatch> _batches;
    private readonly IRepository<InventoryAdjustment> _adjustments;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;
    private readonly IAttachmentUploadService _attachments;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AdjustInventoryCommandHandler(IMedicineVariantRepository variants, IRepository<MedicineBatch> batches,
        IRepository<InventoryAdjustment> adjustments, IUnitOfWork uow,
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

    public async Task<Result<Guid>> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var batch = await _batches.GetByIdAsync(req.MedicineBatchId, tracked: true, cancellationToken: cancellationToken);
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

        var eventVariant = await _variants.GetWithStockGraphAsync(batch.MedicineVariantId, cancellationToken);

        if (eventVariant is not null)
        {
            eventVariant.RaiseLowStockEventIfNeeded(asOf);
        }

        _adjustments.Add(adjustment);
        await _uow.SaveChangesAsync(cancellationToken);

        await _attachments.UploadAsync("InventoryAdjustment", adjustment.Id, req.File, cancellationToken);

        return Result<Guid>.Success(adjustment.Id);
    }
}
