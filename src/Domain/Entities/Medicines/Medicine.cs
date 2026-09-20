using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Medicines;

public class Medicine : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }

    public CategoryEnum CategoryEnum { get; private set; } = CategoryEnum.Other;

    public Guid GenericNameId { get; private set; }
    public GenericName GenericName { get; private set; } = null!;

    public bool IsControlled { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<MedicineVariant> _variants = new();
    public IReadOnlyCollection<MedicineVariant> Variants => _variants.AsReadOnly();

    private Medicine() { }

    public Medicine(string name, CategoryEnum categoryEnum,
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
        IsControlled = isControlled;
    }

    public void UpdateDetails(string name, CategoryEnum categoryEnum,
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
}