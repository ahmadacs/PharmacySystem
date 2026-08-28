using System;

namespace Domain.ValueObjects;

/// <summary>
/// Defines the relationship between a medicine's base (dispensing) unit and its
/// package (purchasing) unit. Batch stock is entered in whole packages and is
/// converted to base units, so the stored base-unit count is always a multiple
/// of <see cref="UnitsPerPackage"/>. <see cref="IsDivisible"/> is documentation
/// metadata only (no enforcement) — a small fraction of medicines (pens, vials,
/// tubes) cannot be split, but splitting decisions remain with the pharmacist.
/// </summary>
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

    /// <summary>
    /// Converts a whole-package count into base units (e.g. 3 boxes of 30 tablets = 90 tablets).
    /// The result is always a multiple of <see cref="UnitsPerPackage"/>.
    /// </summary>
    public Quantity PackagesToBaseUnits(int packages)
    {
        if (packages <= 0)
            throw new ArgumentOutOfRangeException(nameof(packages), "Package count must be positive.");

        return Quantity.Of(packages * UnitsPerPackage);
    }

    public override string ToString() => $"{PackageUnitName} of {UnitsPerPackage} {BaseUnitName}s";
}