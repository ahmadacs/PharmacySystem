using MediatR;

namespace Application.Features.Files.Queries.GetFile;

public sealed record GetFileQuery(Guid FileId) : IRequest<(Stream Content, string ContentType, string FileName)>;
