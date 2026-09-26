using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
using Application.Features.Files.Common;
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
        var fileSpec = new Specification<FileAttachment, FileAttachment>(f => f).Tracked();
        fileSpec.Where(f => f.Id == request.FileId);
        var attachment = await _files.GetAsync(fileSpec, cancellationToken);
        if (attachment is null)
            return Result<(Stream Content, string ContentType, string FileName)>.Failure(_localizer["ResourceNotFound", "FileAttachment", request.FileId].Value, 404);

        var accessFailure = await _access.EnsureCanViewAsync(attachment, cancellationToken);
        if (accessFailure is not null)
            return Result<(Stream Content, string ContentType, string FileName)>.Failure(accessFailure.Error!, accessFailure.StatusCode);

        var (content, contentType) = await _storage.OpenReadAsync(attachment.BlobPath, cancellationToken);
        return Result<(Stream Content, string ContentType, string FileName)>.Success((content, contentType, attachment.FileName));
    }
}
