namespace Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType)> OpenReadAsync(string blobPath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string blobPath, CancellationToken cancellationToken = default);
}
