using Application.Common.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services;

public class ExportService : IExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportToExcel<T>(IReadOnlyList<T> data, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        if (data.Count == 0)
        {
            ws.Cell(1, 1).Value = "No data";
            using var ms0 = new MemoryStream();
            wb.SaveAs(ms0);
            return ms0.ToArray();
        }

        var props = typeof(T).GetProperties();
        for (int c = 0; c < props.Length; c++)
        {
            ws.Cell(1, c + 1).Value = props[c].Name;
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int r = 0; r < data.Count; r++)
        {
            for (int c = 0; c < props.Length; c++)
            {
                var val = props[c].GetValue(data[r]);
                ws.Cell(r + 2, c + 1).Value = val?.ToString() ?? string.Empty;
            }
        }

        ws.Columns().AdjustToContents();
        ws.RangeUsed()!.SetAutoFilter();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToPdf<T>(IReadOnlyList<T> data, string title)
    {
        var props = typeof(T).GetProperties();

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));
                page.Header().Text(title).SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var _ in props) cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var p in props)
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(p.Name).SemiBold().FontSize(7);
                        }
                    });

                    if (data.Count == 0)
                    {
                        table.Cell().ColumnSpan((uint)props.Length).Padding(8).Text("No data").Italic();
                    }
                    else
                    {
                        foreach (var row in data)
                        {
                            foreach (var p in props)
                            {
                                var val = p.GetValue(row)?.ToString() ?? string.Empty;
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(val).FontSize(7);
                            }
                        }
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated ").FontSize(7);
                    x.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").SemiBold().FontSize(7);
                    x.Span(" | Pharmacy System").FontSize(7);
                });
            });
        });

        return pdf.GeneratePdf();
    }
}
