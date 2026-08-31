using System.Globalization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Readers;
using LibraryConnect.Reporting.Barcodes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting.Pdf;

/// <summary>
/// In thẻ bạn đọc ra PDF (VI.2).
///
/// Hai kiểu in vì thư viện in thẻ theo hai cách: đưa phôi thẻ nhựa CR80 vào máy in thẻ thì mỗi thẻ
/// phải là một trang đúng khổ vật lý; in thử hoặc in thẻ giấy ép plastic thì xếp nhiều thẻ trên tờ
/// A4 rồi cắt. Mọi kích thước tính bằng milimét — đó là đơn vị của phôi thẻ và của dao cắt.
/// </summary>
public class ReaderCardPrintService : IReaderCardPrintService
{
    private const float A4WidthMm = 210;
    private const float A4HeightMm = 297;

    /// <summary>Lề tờ A4 khi xếp nhiều thẻ.</summary>
    private const float SheetMarginMm = 10;

    /// <summary>Khoảng cách giữa hai thẻ, đủ đường cắt.</summary>
    private const float GapMm = 4;

    static ReaderCardPrintService()
    {
    }

    public byte[] Render(
        ReaderCardTemplateDto template,
        IReadOnlyList<ReaderCardDataDto> cards,
        CardLibraryInfo library,
        bool multiplePerPage)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(library);

        var barcodes = BuildBarcodes(template, cards);

        return multiplePerPage
            ? RenderSheets(template, cards, library, barcodes)
            : RenderOnePerPage(template, cards, library, barcodes);
    }

    /// <summary>
    /// Ảnh mã vạch dựng sẵn một lần cho mỗi số thẻ: cùng một số thẻ xuất hiện ở cả mặt trước lẫn
    /// mặt sau, mà dựng ảnh là phần tốn thời gian nhất của cả việc in.
    /// </summary>
    private static Dictionary<string, byte[]> BuildBarcodes(
        ReaderCardTemplateDto template, IReadOnlyList<ReaderCardDataDto> cards)
    {
        var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var face in new[] { template.Front, template.Back })
        {
            if (face.Barcode is null)
            {
                continue;
            }

            var widthPx = BarcodeRenderer.MillimetresToPixels(face.Barcode.Width);
            var heightPx = BarcodeRenderer.MillimetresToPixels(face.Barcode.Height);

            foreach (var card in cards)
            {
                var key = $"{face.Barcode.Type}|{card.CardNumber}";

                if (string.IsNullOrWhiteSpace(card.CardNumber) || images.ContainsKey(key))
                {
                    continue;
                }

                images[key] = BarcodeRenderer.Render(card.CardNumber, face.Barcode.Type, widthPx, heightPx);
            }
        }

        return images;
    }

    private static byte[] RenderOnePerPage(
        ReaderCardTemplateDto template,
        IReadOnlyList<ReaderCardDataDto> cards,
        CardLibraryInfo library,
        IReadOnlyDictionary<string, byte[]> barcodes)
    {
        var document = Document.Create(container =>
        {
            foreach (var card in cards)
            {
                AddCardPage(container, template, template.Front, card, library, barcodes);

                if (template.PrintBack)
                {
                    AddCardPage(container, template, template.Back, card, library, barcodes);
                }
            }
        });

        return document.GeneratePdf();
    }

    private static void AddCardPage(
        IDocumentContainer container,
        ReaderCardTemplateDto template,
        CardFaceLayoutDto face,
        ReaderCardDataDto card,
        CardLibraryInfo library,
        IReadOnlyDictionary<string, byte[]> barcodes)
    {
        container.Page(page =>
        {
            page.Size((float)template.WidthMm, (float)template.HeightMm, Unit.Millimetre);
            page.Margin(0);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato));
            page.Content().Element(element => DrawFace(element, face, card, library, barcodes, template));
        });
    }

    private static byte[] RenderSheets(
        ReaderCardTemplateDto template,
        IReadOnlyList<ReaderCardDataDto> cards,
        CardLibraryInfo library,
        IReadOnlyDictionary<string, byte[]> barcodes)
    {
        var columns = Math.Max(1, (int)((A4WidthMm - 2 * SheetMarginMm + GapMm) / (template.WidthMm + GapMm)));
        var rows = Math.Max(1, (int)((A4HeightMm - 2 * SheetMarginMm + GapMm) / (template.HeightMm + GapMm)));

        var perPage = Math.Max(1, Math.Min(columns * rows,
            template.CardsPerPage > 0 ? template.CardsPerPage : columns * rows));

        var document = Document.Create(container =>
        {
            for (var start = 0; start < cards.Count; start += perPage)
            {
                var pageCards = cards.Skip(start).Take(perPage).ToList();

                AddSheet(container, template, template.Front, pageCards, library, barcodes, columns, mirror: false);

                if (template.PrintBack)
                {
                    // Lật thứ tự cột trên trang mặt sau: đưa tờ giấy lật theo cạnh dài vào máy in lần
                    // hai thì mặt sau mới rơi đúng vào mặt trước của cùng một thẻ.
                    AddSheet(container, template, template.Back, pageCards, library, barcodes, columns, mirror: true);
                }
            }
        });

        return document.GeneratePdf();
    }

    private static void AddSheet(
        IDocumentContainer container,
        ReaderCardTemplateDto template,
        CardFaceLayoutDto face,
        IReadOnlyList<ReaderCardDataDto> pageCards,
        CardLibraryInfo library,
        IReadOnlyDictionary<string, byte[]> barcodes,
        int columns,
        bool mirror)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(SheetMarginMm, Unit.Millimetre);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato));

            page.Content().Column(column =>
            {
                for (var offset = 0; offset < pageCards.Count; offset += columns)
                {
                    var slice = pageCards.Skip(offset).Take(columns).ToList();

                    if (mirror)
                    {
                        // Hàng vẫn giữ nguyên vị trí, chỉ đảo trái–phải trong từng hàng.
                        slice.Reverse();

                        // Hàng cuối không đủ cột thì phải chèn ô trống vào bên trái, nếu không thẻ
                        // sẽ bị đẩy sang cột sai khi lật giấy.
                        while (slice.Count < columns && offset + columns > pageCards.Count)
                        {
                            slice.Insert(0, null!);
                        }
                    }

                    column.Item().PaddingBottom(GapMm, Unit.Millimetre).Row(row =>
                    {
                        for (var index = 0; index < slice.Count; index++)
                        {
                            var card = slice[index];

                            var cell = row.ConstantItem((float)template.WidthMm, Unit.Millimetre)
                                .Height((float)template.HeightMm, Unit.Millimetre);

                            if (card is not null)
                            {
                                cell.Element(element =>
                                    DrawFace(element, face, card, library, barcodes, template));
                            }

                            if (index < slice.Count - 1)
                            {
                                row.ConstantItem(GapMm, Unit.Millimetre);
                            }
                        }
                    });
                }
            });
        });
    }

    private static void DrawFace(
        IContainer container,
        CardFaceLayoutDto face,
        ReaderCardDataDto card,
        CardLibraryInfo library,
        IReadOnlyDictionary<string, byte[]> barcodes,
        ReaderCardTemplateDto template)
    {
        var canvas = container
            .Background(face.BackgroundColor ?? Colors.White)
            .Border(0.3f)
            .BorderColor(Colors.Grey.Lighten1);

        canvas.Layers(layers =>
        {
            layers.PrimaryLayer();

            if (face.HeaderBandHeight > 0)
            {
                layers.Layer()
                    .Width((float)template.WidthMm, Unit.Millimetre)
                    .Height((float)face.HeaderBandHeight, Unit.Millimetre)
                    .Background(face.HeaderBandColor ?? Colors.Blue.Darken2);
            }

            foreach (var image in face.Images)
            {
                var bytes = image.Kind == ReaderCardImageKinds.Logo ? library.Logo : card.Photo;

                var slot = layers.Layer()
                    .TranslateX((float)image.X, Unit.Millimetre)
                    .TranslateY((float)image.Y, Unit.Millimetre)
                    .Width((float)image.Width, Unit.Millimetre)
                    .Height((float)image.Height, Unit.Millimetre);

                if (image.Border)
                {
                    slot = slot.Border(0.3f).BorderColor(Colors.Grey.Medium);
                }

                if (bytes is not null && bytes.Length > 0)
                {
                    slot.Padding(0.3f, Unit.Millimetre).Image(bytes).FitArea();
                }
                else if (image.Kind == ReaderCardImageKinds.Photo)
                {
                    // Chưa có ảnh thì in ô trống có viền để dán ảnh vào — vẫn là tấm thẻ dùng được.
                    slot.AlignCenter().AlignMiddle().Text("Ảnh 3×4").FontSize(6).FontColor(Colors.Grey.Medium);
                }
            }

            if (face.Barcode is not null && !string.IsNullOrWhiteSpace(card.CardNumber))
            {
                DrawBarcode(layers, face.Barcode, card, barcodes);
            }

            foreach (var box in face.Boxes)
            {
                var text = ResolveText(box, card, library);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
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
        CardBarcodeDto barcode,
        ReaderCardDataDto card,
        IReadOnlyDictionary<string, byte[]> barcodes)
    {
        if (!barcodes.TryGetValue($"{barcode.Type}|{card.CardNumber}", out var image))
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
                    column.Item()
                        .AlignCenter()
                        .Text(card.CardNumber)
                        .FontSize((float)barcode.FontSize)
                        .LetterSpacing(0.05f);
                }
            });
    }

    private static void DrawBox(IContainer container, CardBoxDto box, string text)
    {
        // Chữ tự co lại cho vừa ô thay vì bị cắt cụt: tên thư viện, tên khoa và họ tên đầy đủ của
        // người Việt thường dài hơn ô đã thiết kế, mà một tấm thẻ in ra mất nửa cái tên là thẻ hỏng.
        var element = box.Border
            ? container.Border(0.3f).BorderColor(Colors.Grey.Lighten1).Padding(0.4f, Unit.Millimetre)
            : container;

        element = element.ScaleToFit();

        element = box.Align switch
        {
            "center" => element.AlignCenter(),
            "right" => element.AlignRight(),
            _ => element
        };

        var span = element.Text(box.Uppercase ? text.ToUpperInvariant() : text)
            .FontSize((float)box.FontSize);

        if (box.Bold)
        {
            span.Bold();
        }

        if (box.Italic)
        {
            span.Italic();
        }

        if (!string.IsNullOrWhiteSpace(box.Color))
        {
            span.FontColor(box.Color);
        }
    }

    private static string ResolveText(CardBoxDto box, ReaderCardDataDto card, CardLibraryInfo library)
    {
        var source = box.Source?.Trim() ?? string.Empty;

        // Nháy kép mở đầu nghĩa là văn bản cố định do người thiết kế gõ, ví dụ nhãn "Họ và tên:".
        if (source.StartsWith('"'))
        {
            return box.Prefix + source.Trim('"');
        }

        var value = source switch
        {
            ReaderCardFields.CardNumber => card.CardNumber,
            ReaderCardFields.FullName => card.FullName,
            ReaderCardFields.StudentCode => card.StudentCode,
            ReaderCardFields.ReaderTypeName => card.ReaderTypeName,
            ReaderCardFields.FacultyName => card.FacultyName,
            ReaderCardFields.MajorName => card.MajorName,
            ReaderCardFields.ClassName => card.ClassName,
            ReaderCardFields.CourseYear => card.CourseYear,
            ReaderCardFields.Gender => card.Gender,
            ReaderCardFields.DateOfBirth => Format(card.DateOfBirth),
            ReaderCardFields.CardIssueDate => Format(card.CardIssueDate),
            ReaderCardFields.CardExpireDate => Format(card.CardExpireDate),
            ReaderCardFields.Email => card.Email,
            ReaderCardFields.Phone => card.Phone,
            ReaderCardFields.Address => card.Address,
            ReaderCardFields.LibraryName => library.Name,
            ReaderCardFields.LibraryAddress => library.Address,
            ReaderCardFields.LibraryPhone => library.Phone,
            _ => null
        };

        return string.IsNullOrWhiteSpace(value) ? string.Empty : box.Prefix + value;
    }

    private static string? Format(DateOnly? date) =>
        date?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Format(DateOnly date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
