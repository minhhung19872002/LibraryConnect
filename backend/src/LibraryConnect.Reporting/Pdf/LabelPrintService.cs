using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Reporting.Barcodes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting.Pdf;

/// <summary>
/// In tem mã vạch và nhãn gáy sách ra PDF (III.2).
///
/// Tờ tem mua sẵn đã cắt sẵn rãnh, nên lưới tem không được suy ra từ khổ giấy mà phải lấy đúng số
/// cột, số hàng, lề trên và lề trái khai trong mẫu — in lệch nửa centimét là hỏng cả tờ tem.
/// </summary>
public class LabelPrintService : ILabelPrintService
{
    private const float A4WidthMm = 210;
    private const float A4HeightMm = 297;

    static LabelPrintService()
    {
    }

    public byte[] RenderBarcodes(BarcodeTemplateDto template, IReadOnlyList<LabelDataDto> items)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(items);

        return RenderSheet(
            items,
            template.Layout,
            template.WidthMm,
            template.HeightMm,
            template.ColumnsPerPage,
            template.RowsPerPage,
            template.MarginTopMm,
            template.MarginLeftMm,
            template.BarcodeType);
    }

    public byte[] RenderLabels(LabelTemplateDto template, IReadOnlyList<LabelDataDto> items)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(items);

        return RenderSheet(
            items,
            template.Layout,
            template.WidthMm,
            template.HeightMm,
            template.ColumnsPerPage,
            template.RowsPerPage,
            template.MarginTopMm,
            template.MarginLeftMm,
            BarcodeType.Code128);
    }

    public byte[] RenderBarcodeImage(string value, BarcodeType type, int widthPx, int heightPx) =>
        BarcodeRenderer.Render(value, type, widthPx, heightPx);

    private static byte[] RenderSheet(
        IReadOnlyList<LabelDataDto> items,
        LabelLayoutDto layout,
        double widthMm,
        double heightMm,
        int columns,
        int rows,
        double marginTopMm,
        double marginLeftMm,
        BarcodeType defaultType)
    {
        columns = Math.Max(1, columns);
        rows = Math.Max(1, rows);
        var perPage = columns * rows;

        // Ảnh mã vạch dựng một lần cho mỗi giá trị: một tờ 40 tem của cùng một lô sách hay lặp lại
        // ký hiệu xếp giá, và dựng ảnh là phần tốn nhất của cả việc in.
        var barcodeImages = BuildBarcodeImages(items, layout, defaultType);

        // Khoảng cách giữa hai tem = phần giấy còn lại chia đều; tờ tem thực tế xếp sát nhau nên
        // thường ra 0, còn tờ A4 in thử thì tem được giãn ra cho dễ cắt.
        //
        // Nửa milimét được chừa lại làm biên an toàn: một mẫu tem lấp vừa khít chiều rộng khả dụng
        // sẽ khiến bộ dựng PDF báo xung đột kích thước vì sai số làm tròn, và cả trang in hỏng.
        const double safetyMm = 0.5;

        var gapX = columns > 1
            ? Math.Max(0, (A4WidthMm - marginLeftMm - safetyMm - columns * widthMm) / (columns - 1))
            : 0;

        var gapY = rows > 1
            ? Math.Max(0, (A4HeightMm - marginTopMm - safetyMm - rows * heightMm) / (rows - 1))
            : 0;

        var document = Document.Create(container =>
        {
            for (var start = 0; start < items.Count; start += perPage)
            {
                var pageItems = items.Skip(start).Take(perPage).ToList();

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato));

                    page.Content()
                        .PaddingTop((float)marginTopMm, Unit.Millimetre)
                        .PaddingLeft((float)marginLeftMm, Unit.Millimetre)
                        .Column(column =>
                        {
                            for (var row = 0; row < rows; row++)
                            {
                                var slice = pageItems.Skip(row * columns).Take(columns).ToList();

                                if (slice.Count == 0)
                                {
                                    break;
                                }

                                column.Item()
                                    .PaddingBottom((float)gapY, Unit.Millimetre)
                                    .Row(rowDescriptor =>
                                    {
                                        for (var index = 0; index < slice.Count; index++)
                                        {
                                            rowDescriptor
                                                .ConstantItem((float)widthMm, Unit.Millimetre)
                                                .Height((float)heightMm, Unit.Millimetre)
                                                .Element(element => DrawLabel(
                                                    element, layout, slice[index], barcodeImages));

                                            if (index < slice.Count - 1 && gapX > 0)
                                            {
                                                rowDescriptor.ConstantItem((float)gapX, Unit.Millimetre);
                                            }
                                        }
                                    });
                            }
                        });
                });
            }
        });

        return document.GeneratePdf();
    }

    private static Dictionary<string, byte[]> BuildBarcodeImages(
        IReadOnlyList<LabelDataDto> items, LabelLayoutDto layout, BarcodeType defaultType)
    {
        var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        if (layout.Barcode is null)
        {
            return images;
        }

        var barcode = layout.Barcode;
        var type = barcode.Type ?? defaultType;
        var widthPx = BarcodeRenderer.MillimetresToPixels(barcode.Width);
        var heightPx = BarcodeRenderer.MillimetresToPixels(barcode.Height);

        foreach (var item in items)
        {
            var value = LabelContentBuilder.ResolveBarcodeValue(item, barcode.Source);

            if (string.IsNullOrWhiteSpace(value) || images.ContainsKey(value))
            {
                continue;
            }

            images[value] = BarcodeRenderer.Render(value, type, widthPx, heightPx);
        }

        return images;
    }

    private static void DrawLabel(
        IContainer container,
        LabelLayoutDto layout,
        LabelDataDto item,
        IReadOnlyDictionary<string, byte[]> barcodeImages)
    {
        var canvas = layout.ShowBorder
            ? container.Border(0.4f).BorderColor(Colors.Grey.Lighten1)
            : container;

        canvas.Padding((float)layout.Padding, Unit.Millimetre).Layers(layers =>
        {
            layers.PrimaryLayer();

            if (layout.Barcode is not null)
            {
                DrawBarcode(layers, layout.Barcode, item, barcodeImages);
            }

            foreach (var box in layout.Boxes)
            {
                var text = LabelContentBuilder.Resolve(item, box.Source);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(box.Prefix))
                {
                    text = box.Prefix + text;
                }

                layers.Layer()
                    .TranslateX((float)box.X, Unit.Millimetre)
                    .TranslateY((float)box.Y, Unit.Millimetre)
                    .Width((float)box.Width, Unit.Millimetre)
                    .Height((float)box.Height, Unit.Millimetre)
                    .Element(element => DrawBox(element, box, text));
            }
        });
    }

    private static void DrawBarcode(
        LayersDescriptor layers,
        LabelBarcodeDto barcode,
        LabelDataDto item,
        IReadOnlyDictionary<string, byte[]> barcodeImages)
    {
        var value = LabelContentBuilder.ResolveBarcodeValue(item, barcode.Source);

        if (string.IsNullOrWhiteSpace(value) || !barcodeImages.TryGetValue(value, out var image))
        {
            return;
        }

        layers.Layer()
            .TranslateX((float)barcode.X, Unit.Millimetre)
            .TranslateY((float)barcode.Y, Unit.Millimetre)
            .Width((float)barcode.Width, Unit.Millimetre)
            .Column(column =>
            {
                column.Item()
                    .Height((float)barcode.Height, Unit.Millimetre)
                    .Image(image).FitArea();

                if (barcode.ShowText)
                {
                    // Dãy số dưới vạch để nhập tay được khi máy quét không đọc — bản sách nào cũng
                    // có ngày mã vạch bị xước.
                    column.Item()
                        .AlignCenter()
                        .Text(value)
                        .FontSize((float)barcode.FontSize)
                        .LetterSpacing(0.05f);
                }
            });
    }

    private static void DrawBox(IContainer container, LabelBoxDto box, string text)
    {
        var element = box.Border
            ? container.Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(0.5f, Unit.Millimetre)
            : container;

        element = box.Align switch
        {
            "center" => element.AlignCenter(),
            "right" => element.AlignRight(),
            _ => element
        };

        var span = element.Text(text).FontSize((float)box.FontSize);

        if (box.Bold)
        {
            span.Bold();
        }

        if (box.Italic)
        {
            span.Italic();
        }
    }
}
