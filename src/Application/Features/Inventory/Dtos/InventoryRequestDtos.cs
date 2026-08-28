using System.ComponentModel.DataAnnotations;
using Application.Common.Attributes;
using Application.Features.Medicines.Dtos;
using Domain.Enums;

namespace Application.Features.Inventory.Dtos;

public sealed record AdjustInventoryRequest
{
    public Guid MedicineBatchId { get; init; }

    [EnumDataType(typeof(InventoryAdjustmentType))]
    public InventoryAdjustmentType Type { get; init; }

    /// <summary>The magnitude of the change; the sign is derived from the type.</summary>
    [PositiveQuantity]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [Required, StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Official stock-in of a brand-new batch (Adjust Stock → Increase/TransferIn).
/// Unlike the Medicines-screen Add Batch, the reason is mandatory here so every
/// receive is fully documented in the audit trail.
/// </summary>
public sealed record ReceiveInventoryRequest
{
    public Guid MedicineVariantId { get; init; }

    [NotInTheFuture]
    public DateOnly ManufactureDate { get; init; }

    [NotInThePast]
    public DateOnly ExpiryDate { get; init; }

    [PositiveQuantity]
    [Range(1, int.MaxValue)]
    public int PackagesReceived { get; init; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal UnitCost { get; init; }

    [StringLength(200)]
    public string? SupplierName { get; init; }

    [Required, StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}