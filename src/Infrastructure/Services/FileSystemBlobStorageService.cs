using Application.Common.Interfaces;
using Application.Common.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class FileSystemBlobStorageService : IFileStorageService
{
    private readonly string _basePath;

    public FileSystemBlobStorageService(IHostEnvironment env, IOptions<FileStorageOptions> options)
    {
        var basePath = options.Value.BasePath;
        _basePath = Path.IsPathRooted(basePath)
            ? basePath
            : Path.Combine(env.ContentRootPath, basePath);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName);
        var blobName = $"{Guid.NewGuid():N}{ext}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var relativePath = Path.Combine(datePath, blobName).Replace("\\", "/");
        var fullPath = Path.Combine(_basePath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(fs, cancellationToken);
        return relativePath;
    }

    public Task<(Stream Content, string ContentType)> OpenReadAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(blobPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Blob not found: {blobPath}", fullPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var contentType = GetContentType(blobPath);
        return Task.FromResult((stream, contentType));
    }

    public Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(blobPath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafeFullPath(blobPath);
        return Task.FromResult(File.Exists(fullPath));
    }

    private string GetSafeFullPath(string blobPath)
    {
        var combined = Path.Combine(_basePath, blobPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        var full = Path.GetFullPath(combined);
        var baseFull = Path.GetFullPath(_basePath);
        if (!full.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !string.Equals(full, baseFull, StringComparison.OrdinalIgnoreCase))
            throw new Domain.Exceptions.FileValidationException("Invalid blob path.");
        return full;
    }

    private static string GetContentType(string blobPath)
    {
        var ext = Path.GetExtension(blobPath).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
