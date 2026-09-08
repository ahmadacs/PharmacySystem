using System.Diagnostics;
using Application.Resources;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        var (statusCode, response) = Map(exception, _localizer);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        else
            _logger.LogWarning(exception, "Request failed for {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static (int StatusCode, ErrorResponse Response) Map(Exception exception, IStringLocalizer<SharedResource> localizer)
    {
        switch (exception)
        {
            case EntityNotFoundException e:
                return (StatusCodes.Status404NotFound,
                    S(localizer["ResourceNotFound", e.EntityType.Name, e.EntityId].Value));
            case ForbiddenResourceException:
                return (StatusCodes.Status403Forbidden, S(localizer["Forbidden"].Value));
            case InvalidCredentialsException:
            case AccountLockedOutException:
            case AccountDisabledException:
            case InvalidRefreshTokenException:
                return (StatusCodes.Status401Unauthorized, S(exception.Message));
            case ConflictingOperationException:
            case RefillNotEligibleException:
            case InvalidPrescriptionStatusException:
                return (StatusCodes.Status409Conflict, S(exception.Message));
            case DbUpdateConcurrencyException:
                return (StatusCodes.Status409Conflict, S(localizer["ConcurrencyConflict"].Value));
            case MissingMedicineVariantException:
                return (StatusCodes.Status400BadRequest, S(exception.Message));
            case InsufficientStockException:
            case ExpiredBatchException:
            case FileValidationException:
            case DomainException:
                return (StatusCodes.Status422UnprocessableEntity, S(exception.Message));
            default:
                return (StatusCodes.Status500InternalServerError, S(localizer["UnexpectedError"].Value));
        }
    }

    private static ErrorResponse S(string message)
        => new()
        {
            Message = message,
            TraceId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N")
        };
}