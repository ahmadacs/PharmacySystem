using System.Diagnostics;
using Application.Common.Models;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WebApi.Common;

namespace WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender => sender;

    /// <summary>Sends a query/command and returns 200 with the result.</summary>
    protected async Task<IActionResult> OkResponse<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);

        // If handler opted into Result<T> pattern, unwrap to proper envelope/status.
        if (result is Result<TResponse> wrapped)
            return wrapped.IsSuccess ? base.Ok(wrapped.Value) : FailureResponse(wrapped.Error!, wrapped.StatusCode);

        return base.Ok(result);
    }

    /// <summary>Overload for handlers that return Result&lt;T&gt; explicitly.</summary>
    protected async Task<IActionResult> OkResponse<T>(IRequest<Result<T>> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        return result.IsSuccess ? base.Ok(result.Value) : FailureResponse(result.Error!, result.StatusCode);
    }


    /// <summary>Sends a command that returns Result (void) — maps failure to error envelope.</summary>
    protected async Task<IActionResult> NoContent(IRequest<Result> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        return result.IsSuccess ? NoContent() : FailureResponse(result.Error!, result.StatusCode);
    }

    /// <summary>Sends a command that returns a response (e.g. Unit) and returns 204.</summary>
    protected async Task<IActionResult> NoContent<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result is Result<TResponse> wrapped)
            return wrapped.IsSuccess ? NoContent() : FailureResponse(wrapped.Error!, wrapped.StatusCode);
        if (result is Result r)
            return r.IsSuccess ? NoContent() : FailureResponse(r.Error!, r.StatusCode);
        await Task.CompletedTask;
        return NoContent();
    }

    /// <summary>Sends a command that returns Result&lt;Guid&gt; and returns 201.</summary>
    protected async Task<IActionResult> Created(string actionName, object routeValues, IRequest<Result<Guid>> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            var versionedRouteValues = new RouteValueDictionary(routeValues);
            var apiVersion = HttpContext.Features.Get<IApiVersioningFeature>()?.RequestedApiVersion;
            if (apiVersion is not null)
                versionedRouteValues["version"] = apiVersion.MajorVersion.HasValue && apiVersion.MinorVersion.GetValueOrDefault() == 0
                    ? apiVersion.MajorVersion.ToString()
                    : apiVersion.ToString();

            return CreatedAtAction(actionName, versionedRouteValues, new { id = result.Value });
        }

        return FailureResponse(result.Error!, result.StatusCode);
    }

    protected ObjectResult FailureResponse(string error, int statusCode = 0)
    {
        var code = statusCode != 0 ? statusCode : MapErrorToStatusCode(error);
        var envelope = new ErrorResponse
        {
            Message = error,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
        return StatusCode(code, envelope);
    }

    protected ObjectResult FailureResponse<T>(Result<T> result) => FailureResponse(result.Error!, result.StatusCode);

    /// <summary>Auth helper: unwraps Result&lt;AuthResponse&gt;, sets refresh cookie on success.</summary>
    protected async Task<IActionResult> AuthResponse(IRequest<Result<Application.Features.Auth.Dtos.AuthResponse>> request, Action<string> setRefreshCookie, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            setRefreshCookie(result.Value.RefreshToken);
            return Ok(result.Value);
        }

        return FailureResponse(result);
    }

    /// <summary>File download helper for GetFile results.</summary>
    protected async Task<IActionResult> FileResponse(IRequest<Result<(Stream Content, string ContentType, string FileName)>> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            var (content, contentType, fileName) = result.Value;
            return File(content, contentType, fileName);
        }

        return FailureResponse(result);
    }

    /// <summary>Export file helper.</summary>
    protected async Task<IActionResult> ExportFileResponse(IRequest<Result<Application.Features.Exports.Queries.ExportFileResult>> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
        }

        return FailureResponse(result);
    }

    /// <summary>Upload helper: returns 201 Created with FileAttachmentDto on success.</summary>
    protected async Task<IActionResult> UploadResponse(IRequest<Result<Application.Features.Files.Dtos.FileAttachmentDto>> request, string actionName, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(actionName, new { id = result.Value.Id }, result.Value);
        }

        return FailureResponse(result);
    }

    // Heuristic mapping so Result.Failure(string) can still return 404/403/409/422 instead of always 400.
    // Handlers that need precise codes can prefix the message or throw typed DomainExceptions (still handled by GlobalExceptionHandler).
    private static int MapErrorToStatusCode(string error)
    {
        var lower = error.ToLowerInvariant();
        if (lower.Contains("not found") || lower.Contains("not exist")) return StatusCodes.Status404NotFound;
        if (lower.Contains("forbidden") || lower.Contains("not authorized") || lower.Contains("permission")) return StatusCodes.Status403Forbidden;
        if (lower.Contains("unauthorized") || lower.Contains("invalid credentials") || lower.Contains("locked out")) return StatusCodes.Status401Unauthorized;
        if (lower.Contains("already exists") || lower.Contains("conflict") || lower.Contains("already dispensed")) return StatusCodes.Status409Conflict;
        if (lower.Contains("insufficient stock") || lower.Contains("expired") || lower.Contains("validation failed")) return StatusCodes.Status422UnprocessableEntity;
        return StatusCodes.Status400BadRequest;
    }
}
