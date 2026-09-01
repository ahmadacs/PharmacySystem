using Application.Common.Models;
using MediatR;

namespace Application.Features.Files.Queries.GetFile;

public sealed record GetFileQuery(Guid FileId) : IRequest<Result<(Stream Content, string ContentType, string FileName)>>;
