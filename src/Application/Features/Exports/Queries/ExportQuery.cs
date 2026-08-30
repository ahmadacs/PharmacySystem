using MediatR;

namespace Application.Features.Exports.Queries;

public sealed record ExportQuery(string EntityType, string Format, string? Id = null) : IRequest<ExportFileResult>;

public sealed record ExportFileResult(byte[] Content, string ContentType, string FileName);
