using Domain.Common;
using Domain.Events;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.Medicines;

public class Medicine : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }

    public CategoryEnum CategoryEnum { get; private set; } = CategoryEnum.Other;

    public Guid GenericNameId { get; private set; }
    public GenericName GenericName { get; private set; } = null!;

    public bool IsControlled { get; private set; }
    public Quantity ReorderLevel { get; private set; } = Quantity.Zero;
    public bool IsActive { get; private set; } = true;

    private readonly List<MedicineVariant> _variants = new();
    public IReadOnlyCollection<MedicineVariant> Variants => _variants.AsReadOnly();

    private Medicine() { }

    public Medicine(string name, CategoryEnum categoryEnum, int reorderLevel,
        GenericName genericName, bool isControlled = false, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Medicine name is required.", nameof(name));
        Name = name.Trim();
        NameAr = nameAr?.Trim();
        CategoryEnum = categoryEnum;
        ArgumentNullException.ThrowIfNull(genericName, nameof(genericName));
        GenericName = genericName;
        GenericNameId = genericName.Id;
        ReorderLevel = Quantity.Of(reorderLevel);
        IsControlled = isControlled;
    }

    public void UpdateDetails(string name, CategoryEnum categoryEnum, int reorderLevel,
        GenericName genericName, bool isControlled, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Medicine name is required.", nameof(name));
        Name = name.Trim();
        NameAr = nameAr?.Trim();
        CategoryEnum = categoryEnum;
        ArgumentNullException.ThrowIfNull(genericName, nameof(genericName));
        GenericName = genericName;
        GenericNameId = genericName.Id;
        ReorderLevel = Quantity.Of(reorderLevel);
        IsControlled = isControlled;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void AddVariant(MedicineVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        if (variant.MedicineId != Id)
            throw new ArgumentException("Variant must belong to this medicine.", nameof(variant));
        _variants.Add(variant);
    }

    public Quantity GetAvailableStock(DateOnly asOf) =>
        _variants
            .NotDeleted()
            .Where(v => v.IsActive)
            .Select(v => v.GetAvailableStock(asOf))
            .Aggregate(Quantity.Zero, (total, q) => total.Add(q));

    public bool IsLowStock(DateOnly asOf) => GetAvailableStock(asOf).Value <= ReorderLevel.Value;

    /// <summary>
    /// Raises MedicineLowStockEvent when the available stock is at or below the
    /// reorder level. Called after a stock-changing operation on one of its batches.
    /// </summary>
    public void RaiseLowStockEventIfNeeded(DateOnly asOf)
    {
        if (!IsLowStock(asOf))
            return;

        RaiseDomainEvent(new MedicineLowStockEvent(
            Id, Name, GetAvailableStock(asOf).Value, ReorderLevel.Value, DateTime.UtcNow));
    }

    public IEnumerable<MedicineBatch> GetExpiredBatches(DateOnly asOf) =>
        _variants.NotDeleted().SelectMany(v => v.GetExpiredBatches(asOf));

    public IEnumerable<MedicineBatch> GetNearExpiryBatches(DateOnly asOf, int withinDays) =>
        _variants.NotDeleted().SelectMany(v => v.GetNearExpiryBatches(asOf, withinDays));
}