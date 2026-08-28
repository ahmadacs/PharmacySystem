using Application.Features.Files.Dtos;
using MediatR;

namespace Application.Features.Files.Queries.ListFiles;

public sealed record ListFilesQuery(string EntityType, Guid EntityId) : IRequest<IReadOnlyList<FileAttachmentDto>>;
