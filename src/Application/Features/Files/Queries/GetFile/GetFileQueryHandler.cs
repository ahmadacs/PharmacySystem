using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Files.Common;
using Application.Features.Files.Dtos;
using Application.Resources;
using Domain.Entities.Files;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Queries.GetFile;

public sealed class GetFileQueryHandler : IRequestHandler<GetFileQuery, Result<(Stream Content, string ContentType, string FileName)>>
{
    private readonly IBaseRepository<FileAttachment> _files;
    private readonly IFileStorageService _storage;
    private readonly IFileAccessChecker _access;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GetFileQueryHandler(
        IBaseRepository<FileAttachment> files,
        IFileStorageService storage,
        IFileAccessChecker access,
        IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
        _storage = storage;
        _access = access;
        _localizer = localizer;
    }

    public async Task<Result<(Stream Content, string ContentType, string FileName)>> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        // Lean selector: only the columns needed for auth + storage open
        // (no full entity, no audit fields).
        var selector = (System.Linq.Expressions.Expression<Func<FileAttachment, FileAttachmentRow>>)(f => new FileAttachmentRow(
            f.Id,
            f.EntityType,
            f.EntityId,
            f.FileName,
            f.BlobPath));

        var row = await _files.GetAsync(selector, f => f.Id == request.FileId, cancellationToken);
        if (row is null)
            return Result<(Stream Content, string ContentType, string FileName)>.Failure(_localizer["ResourceNotFound", "FileAttachment", request.FileId].Value, 404);

        // Transient entity carries only EntityType/EntityId for the access
        // check — preserves the attachment-based auth branch (deleted
        // prescriptions stay viewable) without loading the full row.
        var accessFailure = await _access.EnsureCanViewAsync(
            new FileAttachment(row.EntityType, row.EntityId, row.FileName, "application/octet-stream", 0, row.BlobPath),
            cancellationToken);
        if (accessFailure is not null)
            return Result<(Stream Content, string ContentType, string FileName)>.Failure(accessFailure.Error!, accessFailure.StatusCode);

        var (content, contentType) = await _storage.OpenReadAsync(row.BlobPath, cancellationToken);
        return Result<(Stream Content, string ContentType, string FileName)>.Success((content, contentType, row.FileName));
    }
}
