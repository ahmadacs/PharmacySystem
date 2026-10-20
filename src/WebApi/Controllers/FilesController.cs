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

    /// <summary>
    /// Uploads a file attachment (e.g. prescription document or medicine image) and links it to an entity.
    /// </summary>
    /// <param name="entityType">The owning entity type.</param>
    /// <param name="entityId">The owning entity identifier.</param>
    /// <param name="file">The file to upload. Maximum size is 5 MB.</param>
    [HttpPost("{entityType}/{entityId:guid}")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(string entityType, Guid entityId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return FailureResponse("File is required.", StatusCodes.Status400BadRequest);

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        var command = new UploadFileCommand(entityType, entityId, file.FileName, file.ContentType, file.Length, stream);
        return await UploadResponse(command, nameof(Download), cancellationToken);
    }

    /// <summary>
    /// Downloads a stored file attachment.
    /// </summary>
    /// <param name="id">The file attachment identifier.</param>
    [HttpGet("{id:guid}/download")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        => FileResponse(new GetFileQuery(id), cancellationToken);

    /// <summary>
    /// Lists the file attachments linked to an entity.
    /// </summary>
    /// <param name="entityType">The owning entity type.</param>
    /// <param name="entityId">The owning entity identifier.</param>
    [HttpGet("{entityType}/{entityId:guid}/list")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> List(string entityType, Guid entityId, CancellationToken cancellationToken)
        => OkResponse(new ListFilesQuery(entityType, entityId), cancellationToken);
}
