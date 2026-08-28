using System.Diagnostics;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Common;

namespace WebApi.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, response) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        else
            _logger.LogWarning(exception, "Request failed for {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static (int StatusCode, ErrorResponse Response) Map(Exception exception)
    {
        switch (exception)
        {
            case EntityNotFoundException:
                return (StatusCodes.Status404NotFound, S(exception.Message));
            case ForbiddenResourceException:
                return (StatusCodes.Status403Forbidden, S(exception.Message));
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
                return (StatusCodes.Status409Conflict,
                    S("The record was modified by another request. Refresh and try again."));
            case MissingMedicineVariantException:
                return (StatusCodes.Status400BadRequest, S(exception.Message));
            case InsufficientStockException:
            case ExpiredBatchException:
            case FileValidationException:
            case DomainException:
                return (StatusCodes.Status422UnprocessableEntity, S(exception.Message));
            default:
                return (StatusCodes.Status500InternalServerError, S("An unexpected error occurred."));
        }
    }

    private static ErrorResponse S(string message)
        => new()
        {
            Message = message,
            TraceId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N")
        };
}