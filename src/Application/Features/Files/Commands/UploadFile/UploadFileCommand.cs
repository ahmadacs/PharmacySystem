using Application.Features.Files.Dtos;
using MediatR;

namespace Application.Features.Files.Commands.UploadFile;

public sealed record UploadFileCommand(
    string EntityType,
    Guid EntityId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content
) : IRequest<FileAttachmentDto>;
