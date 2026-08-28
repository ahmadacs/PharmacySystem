using MediatR;

namespace Application.Features.Exports.Queries;

public sealed record ExportQuery(string EntityType, string Format) : IRequest<ExportFileResult>;

public sealed record ExportFileResult(byte[] Content, string ContentType, string FileName);
