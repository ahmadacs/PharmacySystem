using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Security;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Exports.Queries;

public sealed class ExportQueryHandler : IRequestHandler<ExportQuery, Result<ExportFileResult>>
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

    private bool HasPermission(string required, params string[] alternatives)
    {
        var perms = _currentUser.Permissions;
        return perms.Contains(required) || alternatives.Any(a => perms.Contains(a));
    }

    public async Task<Result<ExportFileResult>> Handle(ExportQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Format)) return Result<ExportFileResult>.Failure("Format is required (excel/pdf).", 422);
        if (string.IsNullOrWhiteSpace(request.EntityType)) return Result<ExportFileResult>.Failure("EntityType is required.", 422);
        var format = request.Format.ToLowerInvariant();
        if (format != "excel" && format != "xlsx" && format != "pdf") return Result<ExportFileResult>.Failure($"Invalid format '{request.Format}'. Use excel or pdf.", 422);
        var isExcel = format == "excel" || format == "xlsx";

        var entity = request.EntityType.ToLowerInvariant();
        if (entity == "medicines")
        {
            if (!HasPermission(Permissions.Medicines.View)) return Result<ExportFileResult>.Failure($"Missing required permission: {Permissions.Medicines.View}", 403);
        }
        else if (entity == "inventory")
        {
            if (!HasPermission(Permissions.Inventory.View)) return Result<ExportFileResult>.Failure($"Missing required permission: {Permissions.Inventory.View}", 403);
        }
        else if (entity == "prescriptions")
        {
            if (!HasPermission(Permissions.Prescriptions.View, Permissions.Prescriptions.ManageOwn)) return Result<ExportFileResult>.Failure($"Missing required permission: {Permissions.Prescriptions.View}", 403);
        }
        else if (entity == "dispensing")
        {
            if (!HasPermission(Permissions.Dispensing.View)) return Result<ExportFileResult>.Failure($"Missing required permission: {Permissions.Dispensing.View}", 403);
        }
        else return Result<ExportFileResult>.Failure($"Unknown entity type '{request.EntityType}'. Use medicines/inventory/prescriptions/dispensing.", 422);

        ExportFileResult result;
        try
        {
            if (entity == "medicines") result = await ExportMedicines(isExcel, cancellationToken);
            else if (entity == "inventory") result = await ExportInventory(isExcel, cancellationToken);
            else if (entity == "prescriptions") result = await ExportPrescriptions(isExcel, request.Id, cancellationToken);
            else if (entity == "dispensing") result = await ExportDispensing(isExcel, cancellationToken);
            else return Result<ExportFileResult>.Failure($"Unknown entity type '{request.EntityType}'.", 422);
        }
        catch (FileValidationException ex)
        {
            return Result<ExportFileResult>.Failure(ex.Message, 422);
        }
        catch (ForbiddenResourceException ex)
        {
            return Result<ExportFileResult>.Failure(ex.Message, 403);
        }
        catch (DomainException ex)
        {
            return Result<ExportFileResult>.Failure(ex.Message, 422);
        }

        return Result<ExportFileResult>.Success(result);
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
