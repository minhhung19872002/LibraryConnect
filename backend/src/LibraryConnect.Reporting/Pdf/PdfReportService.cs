using LibraryConnect.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting.Pdf;

/// <summary>
/// Shared table report renderer (QuestPDF). Every "Xuất PDF" button in the product goes through it,
/// so all printed output carries the same header block, column styling and footer.
///
/// The default typeface is Lato, which covers the Vietnamese diacritics; it is embedded by QuestPDF
/// itself, so the container needs no system font installed for reports to render correctly.
/// </summary>
public class PdfReportService : IPdfReportService
{
    private const string PrimaryColor = "#1668dc";
    private const string HeaderBackground = "#E8F0FE";
    private const string BorderColor = "#D9D9D9";

    static PdfReportService()
    {
        // QuestPDF is used here under its Community licence.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] RenderTable<T>(PdfReportHeader header, IReadOnlyList<PdfColumn<T>> columns, IReadOnlyList<T> rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(header.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato).FontSize(9));

                page.Header().Element(element => ComposeHeader(element, header));
                page.Content().PaddingVertical(8).Element(element => ComposeTable(element, columns, rows));
                page.Footer().Element(element => ComposeFooter(element, header));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PdfReportHeader header)
    {
        container.Column(column =>
        {
            column.Item().Text(header.LibraryName.ToUpperInvariant())
                .FontSize(11).Bold().FontColor(PrimaryColor);

            if (!string.IsNullOrWhiteSpace(header.LibraryAddress))
            {
                column.Item().Text(header.LibraryAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
            }

            column.Item().PaddingTop(10).AlignCenter().Text(header.Title.ToUpperInvariant())
                .FontSize(14).Bold();

            if (!string.IsNullOrWhiteSpace(header.Subtitle))
            {
                column.Item().AlignCenter().Text(header.Subtitle).FontSize(10).Italic();
            }

            foreach (var criterion in header.Criteria)
            {
                column.Item().PaddingTop(2).Text(criterion).FontSize(8).FontColor(Colors.Grey.Darken2);
            }

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(PrimaryColor);
        });
    }

    private static void ComposeTable<T>(IContainer container, IReadOnlyList<PdfColumn<T>> columns, IReadOnlyList<T> rows)
    {
        if (rows.Count == 0)
        {
            container.AlignCenter().PaddingVertical(30)
                .Text("Không có dữ liệu phù hợp với điều kiện lọc.")
                .FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(definition =>
            {
                // A narrow ordinal column, then the caller's columns by relative width.
                definition.ConstantColumn(28);

                foreach (var column in columns)
                {
                    definition.RelativeColumn(column.Width);
                }
            });

            // Repeated on every page so a multi-page report stays readable.
            table.Header(headerRow =>
            {
                headerRow.Cell().Element(HeaderCell).Text("TT").Bold();

                foreach (var column in columns)
                {
                    headerRow.Cell().Element(HeaderCell).Text(column.Header).Bold();
                }
            });

            var index = 1;

            foreach (var row in rows)
            {
                var isEven = index % 2 == 0;

                table.Cell().Element(cell => BodyCell(cell, isEven)).AlignCenter().Text(index.ToString());

                foreach (var column in columns)
                {
                    var cellContainer = table.Cell().Element(cell => BodyCell(cell, isEven));

                    cellContainer = column.Align switch
                    {
                        PdfAlign.Center => cellContainer.AlignCenter(),
                        PdfAlign.Right => cellContainer.AlignRight(),
                        _ => cellContainer
                    };

                    cellContainer.Text(column.Value(row));
                }

                index++;
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(HeaderBackground)
            .Border(0.5f)
            .BorderColor(BorderColor)
            .Padding(4)
            .AlignMiddle();

    private static IContainer BodyCell(IContainer container, bool isEven) =>
        container
            .Background(isEven ? "#FAFAFA" : Colors.White)
            .Border(0.5f)
            .BorderColor(BorderColor)
            .Padding(4)
            .AlignMiddle();

    private static void ComposeFooter(IContainer container, PdfReportHeader header)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(BorderColor);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                    text.Span($"Xuất lúc {DateTimeOffset.Now:HH:mm dd/MM/yyyy}");

                    if (!string.IsNullOrWhiteSpace(header.PreparedBy))
                    {
                        text.Span($" · Người xuất: {header.PreparedBy}");
                    }
                });

                row.ConstantItem(120).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }
}
