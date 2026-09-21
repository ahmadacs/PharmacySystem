using Application.Resources;
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
            InvalidCredentialsException or InvalidRefreshTokenException =>
                (StatusCodes.Status401Unauthorized, exception.Message),
            ConflictingOperationException or RefillNotEligibleException or InvalidPrescriptionStatusException =>
                (StatusCodes.Status409Conflict, exception.Message),
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, localizer["ConcurrencyConflict"]),
            MissingMedicineVariantException =>
                (StatusCodes.Status400BadRequest, exception.Message),
            DomainException =>
                (StatusCodes.Status422UnprocessableEntity, exception.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, localizer["UnexpectedError"])
        };
}
