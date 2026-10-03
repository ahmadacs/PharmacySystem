using System.Linq.Expressions;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Files.Common;
using Application.Features.Files.Dtos;
using Application.Resources;
using Domain.Entities.Files;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Application.Features.Files.Queries.ListFiles;

public sealed class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, Result<IReadOnlyList<FileAttachmentDto>>>
{
    private readonly IRepository<FileAttachment> _files;
    private readonly IFileAccessChecker _access;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListFilesQueryHandler(
        IRepository<FileAttachment> files,
        IFileAccessChecker access,
        IStringLocalizer<SharedResource> localizer)
    {
        _files = files;
        _access = access;
        _localizer = localizer;
    }

    public async Task<Result<IReadOnlyList<FileAttachmentDto>>> Handle(ListFilesQuery request, CancellationToken cancellationToken)
    {
        if (request.EntityId == Guid.Empty)
            return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["EntityIdRequired"].Value, 422);
        if (!Enum.TryParse<FileEntityType>(request.EntityType, true, out var entityType))
            return Result<IReadOnlyList<FileAttachmentDto>>.Failure(_localizer["InvalidEntityType", request.EntityType, "Medicine, Prescription, Batch, InventoryAdjustment"].Value, 422);

        var accessFailure = await _access.EnsureCanViewAsync(entityType, request.EntityId, cancellationToken);
        if (accessFailure is not null)
            return Result<IReadOnlyList<FileAttachmentDto>>.Failure(accessFailure.Error!, accessFailure.StatusCode);

        Expression<Func<FileAttachment, FileAttachmentDto>> selector = f => new FileAttachmentDto(
            f.Id,
            f.EntityType.ToString(),
            f.EntityId,
            f.FileName,
            f.ContentType,
            f.SizeBytes,
            f.BlobPath,
            f.CreatedAt);

        Expression<Func<FileAttachment, bool>> predicate =
            f => f.EntityType == entityType && f.EntityId == request.EntityId;

        var list = (await _files.ListReadAsync(selector, predicate, cancellationToken))
            .OrderByDirection(f => f.CreatedAt, "desc")
            .ToList();
        return Result<IReadOnlyList<FileAttachmentDto>>.Success(list);
    }
}
