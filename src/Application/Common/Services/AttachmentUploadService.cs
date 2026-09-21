using Application.Common.Interfaces;
using Application.Features.Files.Commands.UploadFile;
using Application.Features.Inventory.Dtos;
using MediatR;

namespace Application.Common.Services;

/// <summary>Decodes an optional base64 payload and stores it via <see cref="UploadFileCommand"/>.</summary>
public sealed class AttachmentUploadService : IAttachmentUploadService
{
    private readonly ISender _sender;

    public AttachmentUploadService(ISender sender)
    {
        _sender = sender;
    }

    public async Task UploadAsync(
        string entityType,
        Guid entityId,
        FileUploadDto? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || string.IsNullOrWhiteSpace(file.Base64Content))
            return;

        var fileBytes = Convert.FromBase64String(file.Base64Content);
        using var stream = new MemoryStream(fileBytes);
        await _sender.Send(new UploadFileCommand(
            entityType,
            entityId,
            file.FileName,
            file.ContentType,
            file.SizeBytes,
            stream), cancellationToken);
    }
}
