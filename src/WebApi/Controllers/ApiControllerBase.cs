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

    protected async Task<IActionResult> OkResponse<T>(IRequest<Result<T>> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        return result.IsSuccess ? base.Ok(result.Value) : FailureResponse(result.Error!, result.StatusCode);
    }

    protected async Task<IActionResult> NoContent(IRequest<Result> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        return result.IsSuccess ? NoContent() : FailureResponse(result.Error!, result.StatusCode);
    }

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

    protected ObjectResult FailureResponse(string error, int statusCode)
    {
        var envelope = new ErrorResponse
        {
            Message = error,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
        return StatusCode(statusCode, envelope);
    }

    protected ObjectResult FailureResponse<T>(Result<T> result) => FailureResponse(result.Error!, result.StatusCode);

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

    protected async Task<IActionResult> ExportFileResponse(IRequest<Result<Application.Features.Exports.Queries.ExportFileResult>> request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
        }

        return FailureResponse(result);
    }

    protected async Task<IActionResult> UploadResponse(IRequest<Result<Application.Features.Files.Dtos.FileAttachmentDto>> request, string actionName, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(actionName, new { id = result.Value.Id }, result.Value);
        }

        return FailureResponse(result);
    }
}
