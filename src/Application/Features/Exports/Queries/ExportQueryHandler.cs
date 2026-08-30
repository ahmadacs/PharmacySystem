using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Exports.Queries;

public sealed class ExportQueryHandler : IRequestHandler<ExportQuery, ExportFileResult>
{
    private readonly IExportDataProvider _provider;
    private readonly IExportService _export;
    private readonly ICurrentUserService _currentUser;

    public ExportQueryHandler(IExportDataProvider provider, IExportService export, ICurrentUserService currentUser)
    {
        _provider = provider;
        _export = export;
        _currentUser = currentUser;
    }

    private void EnsurePermission(string required, params string[] alternatives)
    {
        var perms = _currentUser.Permissions;
        if (perms.Contains(required) || alternatives.Any(a => perms.Contains(a))) return;
        throw new ForbiddenResourceException($"Missing required permission: {required}");
    }

    public async Task<ExportFileResult> Handle(ExportQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Format)) throw new FileValidationException("Format is required (excel/pdf).");
        if (string.IsNullOrWhiteSpace(request.EntityType)) throw new FileValidationException("EntityType is required.");
        var format = request.Format.ToLowerInvariant();
        if (format != "excel" && format != "xlsx" && format != "pdf") throw new FileValidationException($"Invalid format '{request.Format}'. Use excel or pdf.");
        var isExcel = format == "excel" || format == "xlsx";

        var entity = request.EntityType.ToLowerInvariant();
        if (entity == "medicines") EnsurePermission(Permissions.Medicines.View);
        else if (entity == "inventory") EnsurePermission(Permissions.Inventory.View);
        else if (entity == "prescriptions") EnsurePermission(Permissions.Prescriptions.View, Permissions.Prescriptions.ManageOwn);
        else if (entity == "dispensing") EnsurePermission(Permissions.Dispensing.View);
        else throw new FileValidationException($"Unknown entity type '{request.EntityType}'. Use medicines/inventory/prescriptions/dispensing.");

        return entity switch
        {
            "medicines" => await ExportMedicines(isExcel, cancellationToken),
            "inventory" => await ExportInventory(isExcel, cancellationToken),
            "prescriptions" => await ExportPrescriptions(isExcel, request.Id, cancellationToken),
            "dispensing" => await ExportDispensing(isExcel, cancellationToken),
            _ => throw new FileValidationException($"Unknown entity type '{request.EntityType}'.")
        };
    }

    private async Task<ExportFileResult> ExportMedicines(bool isExcel, CancellationToken ct)
    {
        var data = await _provider.GetMedicinesAsync(ct);
        var title = "Medicines Report";
        var bytes = isExcel ? _export.ExportToExcel(data, "Medicines") : _export.ExportToPdf(data, title);
        return new ExportFileResult(bytes, isExcel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf", isExcel ? "medicines.xlsx" : "medicines.pdf");
    }

    private async Task<ExportFileResult> ExportInventory(bool isExcel, CancellationToken ct)
    {
        var data = await _provider.GetInventoryAsync(ct);
        var title = "Inventory Report";
        var bytes = isExcel ? _export.ExportToExcel(data, "Inventory") : _export.ExportToPdf(data, title);
        return new ExportFileResult(bytes, isExcel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf", isExcel ? "inventory.xlsx" : "inventory.pdf");
    }

    private async Task<ExportFileResult> ExportPrescriptions(bool isExcel, string? id, CancellationToken ct)
    {
        var data = await _provider.GetPrescriptionsAsync(ct, id);
        var title = !string.IsNullOrEmpty(id) ? "Prescription Report" : "Prescriptions Report";
        var bytes = isExcel ? _export.ExportToExcel(data, "Prescriptions") : _export.ExportToPdf(data, title);
        return new ExportFileResult(bytes, isExcel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf", isExcel ? "prescriptions.xlsx" : "prescriptions.pdf");
    }

    private async Task<ExportFileResult> ExportDispensing(bool isExcel, CancellationToken ct)
    {
        var data = await _provider.GetDispensingAsync(ct);
        var title = "Dispensing History";
        var bytes = isExcel ? _export.ExportToExcel(data, "Dispensing") : _export.ExportToPdf(data, title);
        return new ExportFileResult(bytes, isExcel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf", isExcel ? "dispensing.xlsx" : "dispensing.pdf");
    }
}
