using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
        return base.Ok(result);
    }

    /// <summary>Sends a command that returns nothing and returns 204.</summary>
    protected async Task<IActionResult> NoContent(IRequest request, CancellationToken cancellationToken)
    {
        await Sender.Send(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Sends a command that returns a response (e.g. Unit) and returns 204.</summary>
    protected async Task<IActionResult> NoContent<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        await Sender.Send(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Sends a command that returns an id and returns 201 with the location header.</summary>
    protected async Task<IActionResult> Created(string actionName, object routeValues, IRequest<Guid> request, CancellationToken cancellationToken)
    {
        var id = await Sender.Send(request, cancellationToken);

        var versionedRouteValues = new RouteValueDictionary(routeValues);
        var apiVersion = HttpContext.Features.Get<IApiVersioningFeature>()?.RequestedApiVersion;
        if (apiVersion is not null)
            versionedRouteValues["version"] = apiVersion.MajorVersion.HasValue && apiVersion.MinorVersion.GetValueOrDefault() == 0
                ? apiVersion.MajorVersion.ToString()
                : apiVersion.ToString();

        return CreatedAtAction(actionName, versionedRouteValues, new { id });
    }
}
