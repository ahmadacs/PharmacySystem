using Application.Common.Interfaces;
using Application.Common.Options;
using Application.Features.Files.Commands.UploadFile;
using Application.Features.Inventory.Dtos;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Inventory.Commands;

public sealed class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, Guid>
{
    private readonly IMedicineRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationOptions _notificationOptions;
    private readonly ISender _sender;

    public AdjustInventoryCommandHandler(IMedicineRepository repo, IUnitOfWork uow,
        ICurrentUserService currentUser, NotificationOptions notificationOptions, ISender sender)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _notificationOptions = notificationOptions;
        _sender = sender;
    }

    public async Task<Guid> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var batch = await _repo.GetBatchByIdAsync(req.MedicineBatchId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(MedicineBatch), req.MedicineBatchId);

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

        var medicines = await _repo.GetMedicinesByVariantIdsForStockCheckAsync([batch.MedicineVariantId], cancellationToken);
        foreach (var medicine in medicines)
            medicine.RaiseLowStockEventIfNeeded(asOf);

        _repo.AddAdjustment(adjustment);
        await _uow.SaveChangesAsync(cancellationToken);

        // Upload file if provided
        if (req.File is not null && !string.IsNullOrWhiteSpace(req.File.Base64Content))
        {
            var fileBytes = Convert.FromBase64String(req.File.Base64Content);
            using var stream = new MemoryStream(fileBytes);
            await _sender.Send(new UploadFileCommand(
                "InventoryAdjustment",
                adjustment.Id,
                req.File.FileName,
                req.File.ContentType,
                req.File.SizeBytes,
                stream), cancellationToken);
        }

        return adjustment.Id;
    }
}