using System.Globalization;
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
        // NOTE: never use "yyyy/MM/dd" formatting here — "/" is a
        // culture-sensitive date-separator placeholder and Arabic cultures
        // emit U+200F (RLM) around it, producing unreadable blob paths
        // (seen in prod: "2026‏/09‏/21/..."). Build segments explicitly.
        var utcNow = DateTime.UtcNow;
        var relativePath = string.Join("/",
            utcNow.ToString("yyyy", CultureInfo.InvariantCulture),
            utcNow.ToString("MM", CultureInfo.InvariantCulture),
            utcNow.ToString("dd", CultureInfo.InvariantCulture),
            blobName);
        relativePath = StripFormatCharacters(relativePath);
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
        {
            // Legacy files were physically saved under RLM-injected directory
            // names (e.g. "2026‏/09‏/21/..."). Fall back to the raw path so
            // those rows keep resolving until data is repaired.
            var rawFullPath = GetSafeFullPath(blobPath, sanitize: false);
            if (!string.Equals(rawFullPath, fullPath, StringComparison.Ordinal) && File.Exists(rawFullPath))
                fullPath = rawFullPath;
        }
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
        // Also remove the legacy RLM-named copy if present.
        var rawFullPath = GetSafeFullPath(blobPath, sanitize: false);
        if (!string.Equals(rawFullPath, fullPath, StringComparison.Ordinal) && File.Exists(rawFullPath))
            File.Delete(rawFullPath);
        return Task.CompletedTask;
    }

    private string GetSafeFullPath(string blobPath, bool sanitize = true)
    {
        // Sanitize once here so both new and legacy (RLM-injected) rows resolve.
        if (sanitize)
            blobPath = StripFormatCharacters(blobPath);
        var combined = Path.Combine(_basePath, blobPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        var full = Path.GetFullPath(combined);
        var baseFull = Path.GetFullPath(_basePath);
        if (!full.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !string.Equals(full, baseFull, StringComparison.OrdinalIgnoreCase))
            throw new Domain.Exceptions.FileValidationException("Invalid blob path.");
        return full;
    }

    /// <summary>
    /// Removes invisible Unicode format characters (RLM U+200F and friends)
    /// that Arabic-culture date formatting once injected into blob paths.
    /// New paths use InvariantCulture; this stays for legacy rows.
    /// </summary>
    private static string StripFormatCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var ch in value)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control)
                continue;
            if (ch is '\u061C' or '\u200E' or '\u200F' or '\uFEFF')
                continue;
            sb.Append(ch);
        }
        return sb.ToString();
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
