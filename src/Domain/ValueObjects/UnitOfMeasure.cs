using System;

namespace Domain.ValueObjects;

public sealed record UnitOfMeasure
{
    public string BaseUnitName { get; }
    public string PackageUnitName { get; }
    public int UnitsPerPackage { get; }
    public bool IsDivisible { get; }

    private UnitOfMeasure(string baseUnitName, string packageUnitName, int unitsPerPackage, bool isDivisible)
    {
        BaseUnitName = baseUnitName;
        PackageUnitName = packageUnitName;
        UnitsPerPackage = unitsPerPackage;
        IsDivisible = isDivisible;
    }

    public static UnitOfMeasure Create(string baseUnit, string packageUnit, int unitsPerPackage, bool isDivisible = true)
    {
        if (string.IsNullOrWhiteSpace(baseUnit))
            throw new ArgumentException("Base unit name is required.", nameof(baseUnit));
        if (string.IsNullOrWhiteSpace(packageUnit))
            throw new ArgumentException("Package unit name is required.", nameof(packageUnit));
        if (unitsPerPackage <= 0)
            throw new ArgumentException("Units per package must be positive.", nameof(unitsPerPackage));

        return new UnitOfMeasure(baseUnit.Trim(), packageUnit.Trim(), unitsPerPackage, isDivisible);
    }

    public Quantity PackagesToBaseUnits(int packages)
    {
        if (packages <= 0)
            throw new ArgumentOutOfRangeException(nameof(packages), "Package count must be positive.");

        return Quantity.Of(packages * UnitsPerPackage);
    }

    public override string ToString() => $"{PackageUnitName} of {UnitsPerPackage} {BaseUnitName}s";
}