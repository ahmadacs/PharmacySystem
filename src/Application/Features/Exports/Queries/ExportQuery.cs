using Application.Common.Models;
using MediatR;

namespace Application.Features.Exports.Queries;

public sealed record ExportQuery(string EntityType, string Format, string? Id = null) : IRequest<Result<ExportFileResult>>;

public sealed record ExportFileResult(byte[] Content, string ContentType, string FileName);
