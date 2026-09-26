using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Specifications;
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
    private readonly IBaseRepository<FileAttachment> _files;
    private readonly IFileAccessChecker _access;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListFilesQueryHandler(
        IBaseRepository<FileAttachment> files,
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

        var listSpec = new Specification<FileAttachment, FileAttachment>(f => f);
        listSpec.Where(f => f.EntityType == entityType && f.EntityId == request.EntityId);
        listSpec.Order(q => q.OrderByDescending(f => f.CreatedAt));
        var list = await _files.ListAsync(listSpec, cancellationToken);
        return Result<IReadOnlyList<FileAttachmentDto>>.Success(list.Select(x => x.ToDto()).ToList());
    }
}
