using Application.Common.Security;
using Application.Features.Files.Commands.UploadFile;
using Application.Features.Files.Queries.GetFile;
using Application.Features.Files.Queries.ListFiles;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiVersion("1.0")]
public sealed class FilesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Uploads a file for a medicine or prescription (jpeg/png/pdf, max 5MB).</summary>
    [HttpPost("{entityType}/{entityId:guid}")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(string entityType, Guid entityId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is required." });

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        var command = new UploadFileCommand(entityType, entityId, file.FileName, file.ContentType, file.Length, stream);
        return await UploadResponse(command, nameof(Get), cancellationToken);
    }

    /// <summary>Gets file metadata by id.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFileQuery(id), cancellationToken);
        if (result.IsSuccess)
        {
            var (content, contentType, fileName) = result.Value;
            return File(content, contentType, fileName);
        }

        return FailureResponse(result);
    }

    /// <summary>Downloads file content.</summary>
    [HttpGet("{id:guid}/download")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetFileQuery(id), cancellationToken);
        if (result.IsSuccess)
        {
            var (content, contentType, fileName) = result.Value;
            return File(content, contentType, fileName);
        }

        return FailureResponse(result);
    }

    /// <summary>Lists files for an entity.</summary>
    [HttpGet("{entityType}/{entityId:guid}/list")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> List(string entityType, Guid entityId, CancellationToken cancellationToken)
        => OkResponse(new ListFilesQuery(entityType, entityId), cancellationToken);
}
