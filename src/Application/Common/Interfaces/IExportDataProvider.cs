namespace Application.Common.Interfaces;

public interface IExportDataProvider
{
    Task<IReadOnlyList<MedicineExportRow>> GetMedicinesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InventoryExportRow>> GetInventoryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PrescriptionExportRow>> GetPrescriptionsAsync(CancellationToken ct = default, string? id = null);
    Task<IReadOnlyList<DispensingExportRow>> GetDispensingAsync(CancellationToken ct = default);
}

public sealed record MedicineExportRow(string Name, string GenericName, string Category, string Form, string Strength, int Stock, bool IsActive);
public sealed record InventoryExportRow(string Medicine, string Variant, string BatchNumber, int Available, int Reserved, DateOnly Expiry, string Status);
public sealed record PrescriptionExportRow(string PrescriptionNumber, string Patient, string Doctor, string Status, DateTime IssueDate, int ItemsCount, string ItemsDescription);
public sealed record DispensingExportRow(string PrescriptionNumber, string Medicine, int Quantity, DateTime DispensedAt, string DispensedBy);
