using System.Globalization;
using Application.Common.Extensions;
using Application.Features.Prescriptions.Common;
using Application.Resources;
using Domain.Entities.Medicines;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebApi.Common;

namespace WebApi.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IStringLocalizer<SharedResource> localizer)
    {
        _logger = logger;
        _localizer = localizer;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = Map(exception, _localizer);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        else
            _logger.LogWarning(exception, "Request failed for {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse { Message = message, TraceId = httpContext.TraceIdentifier },
            cancellationToken);

        return true;
    }

    private static (int StatusCode, string Message) Map(Exception exception, IStringLocalizer<SharedResource> localizer) =>
        exception switch
        {
            EntityNotFoundException e =>
                (StatusCodes.Status404NotFound, localizer["ResourceNotFound", e.EntityType.Name, e.EntityId]),
            ForbiddenResourceException =>
                (StatusCodes.Status403Forbidden, localizer["Forbidden"]),
            InvalidCredentialsException =>
                (StatusCodes.Status401Unauthorized, localizer["EmailOrPasswordIncorrect"]),
            InvalidRefreshTokenException =>
                (StatusCodes.Status401Unauthorized, localizer["RefreshTokenInvalid"]),
            ConflictingOperationException =>
                (StatusCodes.Status409Conflict, exception.Message),
            FileNotFoundException =>
                (StatusCodes.Status404NotFound, localizer["BlobNotFound"]),
            InvalidBatchDatesException =>
                (StatusCodes.Status422UnprocessableEntity, localizer["ExpiryAfterManufacture"]),
            MissingMedicineVariantException e =>
                (StatusCodes.Status404NotFound, localizer["ResourceNotFound", nameof(MedicineVariant), e.MedicineVariantId]),
            InsufficientStockException e =>
                (StatusCodes.Status409Conflict, localizer["InsufficientStockVariant",
                    MedicineDisplayNames.Resolve(e.MedicineName, e.MedicineNameAr,
                        CultureInfo.CurrentUICulture, e.MedicineBatchId.ToString()),
                    e.Requested, e.Available]),
            ExpiredBatchException e =>
                (StatusCodes.Status409Conflict, localizer["ExpiredBatchCannotDispense",
                    e.BatchNumber ?? e.MedicineBatchId.ToString(),
                    e.ExpiryDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)]),
            RefillIntervalNotSatisfiedException e =>
                (StatusCodes.Status409Conflict, localizer["RefillNotDueForDispense",
                    MedicineDisplayNames.Resolve(e.MedicineName, e.MedicineNameAr,
                        CultureInfo.CurrentUICulture, e.PrescriptionItemId.ToString()),
                    e.NextEligibleDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)]),
            RefillNotEligibleException e =>
                (StatusCodes.Status409Conflict, MapRefillNotEligible(e, localizer)),
            InvalidPrescriptionStatusException e =>
                (StatusCodes.Status409Conflict, MapPrescriptionStatus(e, localizer)),
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, localizer["ConcurrencyConflict"]),
            DomainException =>
                (StatusCodes.Status422UnprocessableEntity, exception.Message),
            ArgumentException =>
                (StatusCodes.Status400BadRequest, exception.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, localizer["UnexpectedError"])
        };

    private static string MapRefillNotEligible(RefillNotEligibleException e, IStringLocalizer<SharedResource> localizer) =>
        e.Reason switch
        {
            RefillEligibilityReason.NotRefillable =>
                localizer["RefillItemNotRefillable", e.PrescriptionItemId],
            RefillEligibilityReason.NotFullyDispensed =>
                localizer["RefillItemNotDispensed", e.PrescriptionItemId],
            RefillEligibilityReason.Exhausted =>
                localizer["RefillItemExhausted", e.PrescriptionItemId, e.RefillsUsed, e.RefillsAllowed],
            _ => e.Message
        };

    private static string MapPrescriptionStatus(InvalidPrescriptionStatusException e, IStringLocalizer<SharedResource> localizer) =>
        e.Reason switch
        {
            PrescriptionStatusReason.AddItemsProhibited =>
                localizer["PrescriptionCannotAddItems", PrescriptionStatusDisplay.ToDisplayName(e.Status, localizer)],
            PrescriptionStatusReason.AlreadyCancelled =>
                localizer["AlreadyCancelled"],
            PrescriptionStatusReason.CancelAfterDispensed =>
                localizer["CannotCancelDispensed"],
            PrescriptionStatusReason.NotDispensable =>
                localizer["PrescriptionNotDispensable", PrescriptionStatusDisplay.ToDisplayName(e.Status, localizer)],
            PrescriptionStatusReason.Empty =>
                localizer["PrescriptionHasNoItems"],
            PrescriptionStatusReason.ItemNotInPrescription =>
                localizer["RefillItemNotInPrescription", e.PrescriptionItemId?.ToString() ?? string.Empty, e.PrescriptionId],
            PrescriptionStatusReason.RefillProhibited =>
                localizer["RefillPrescriptionStatus", e.PrescriptionId, PrescriptionStatusDisplay.ToDisplayName(e.Status, localizer)],
            _ => e.Message
        };
}
