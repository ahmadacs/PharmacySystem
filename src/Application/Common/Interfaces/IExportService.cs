namespace Application.Common.Interfaces;

public interface IExportService
{
    byte[] ExportToExcel<T>(IReadOnlyList<T> data, string sheetName);
    byte[] ExportToPdf<T>(IReadOnlyList<T> data, string title);
}

public record ExportResult(byte[] Content, string ContentType, string FileName);
