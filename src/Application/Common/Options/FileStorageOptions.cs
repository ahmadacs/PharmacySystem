namespace Application.Common.Options;

public class FileStorageOptions
{
    public const string SectionName = "Storage";
    public string BasePath { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
