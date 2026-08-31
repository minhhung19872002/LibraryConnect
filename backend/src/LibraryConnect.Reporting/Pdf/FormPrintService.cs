using System.Globalization;
using System.Text.RegularExpressions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryConnect.Reporting.Pdf;

/// <summary>
/// Kết xuất biểu mẫu nghiệp vụ ra PDF (III.6).
///
/// Bố cục theo đúng thói quen văn bản hành chính Việt Nam: tên cơ quan bên trái, quốc hiệu và tiêu
/// ngữ bên phải, tên biểu mẫu in hoa ở giữa, phần thông tin, bảng chi tiết, rồi các ô ký. Người
/// thiết kế đổi được nội dung từng phần nhưng không đổi được trật tự đó, vì trật tự là cái làm cho
/// tờ giấy được chấp nhận khi mang đi ký.
/// </summary>
public class FormPrintService : IFormPrintService
{
    private static readonly Regex Placeholder = new(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    static FormPrintService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(FormTemplateDto template, FormDataDto data, byte[]? logo = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(data);

        var layout = template.Layout;
        var fontSize = (float)(layout.FontSize <= 0 ? 10 : layout.FontSize);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSize(page, template);
                page.Margin(15, Unit.Millimetre);
                page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato).FontSize(fontSize));

                page.Content().Column(column =>
                {
                    column.Spacing(6);

                    DrawHeading(column, layout, data, logo, fontSize);
                    DrawTitle(column, layout, data, fontSize);
                    DrawIntro(column, layout, data);
                    DrawFields(column, layout, data);
                    DrawTable(column, layout, data, fontSize);
                    DrawClosing(column, layout, data);
                    DrawSignatures(column, layout, data, fontSize);
                });

                if (!string.IsNullOrWhiteSpace(layout.Footer))
                {
                    page.Footer().AlignCenter().Text(Resolve(layout.Footer, data))
                        .FontSize(fontSize - 2).FontColor(Colors.Grey.Darken1);
                }
            });
        });

        return document.GeneratePdf();
    }

    private static void ApplyPageSize(PageDescriptor page, FormTemplateDto template)
    {
        if (template.CustomWidthMm is > 0 && template.CustomHeightMm is > 0)
        {
            page.Size((float)template.CustomWidthMm.Value, (float)template.CustomHeightMm.Value, Unit.Millimetre);
            return;
        }

        var size = template.PaperSize?.Trim().ToUpperInvariant() switch
        {
            "A5" => PageSizes.A5,
            _ => PageSizes.A4
        };

        page.Size(template.IsLandscape ? size.Landscape() : size);
    }

    private static void DrawHeading(
        ColumnDescriptor column, FormLayoutDto layout, FormDataDto data, byte[]? logo, float fontSize)
    {
        if (layout.OrganisationLines.Count == 0 && !layout.ShowNationalHeading && logo is null)
        {
            return;
        }

        column.Item().Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                if (layout.ShowLogo && logo is not null)
                {
                    left.Item().Height(18, Unit.Millimetre).AlignLeft().Image(logo).FitHeight();
                    left.Item().Height(2, Unit.Millimetre);
                }

                foreach (var line in layout.OrganisationLines)
                {
                    var text = Resolve(line, data);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    left.Item().AlignCenter().Text(text).Bold().FontSize(fontSize);
                }

                // Gạch chân dưới tên cơ quan — dấu hiệu quen thuộc của văn bản hành chính.
                if (layout.OrganisationLines.Count > 0)
                {
                    left.Item().AlignCenter().Width(45, Unit.Millimetre)
                        .BorderBottom(0.8f).BorderColor(Colors.Black);
                }
            });

            if (!layout.ShowNationalHeading)
            {
                return;
            }

            row.RelativeItem().Column(right =>
            {
                right.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM")
                    .Bold().FontSize(fontSize);
                right.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc")
                    .Bold().FontSize(fontSize);
                right.Item().AlignCenter().Width(55, Unit.Millimetre)
                    .BorderBottom(0.8f).BorderColor(Colors.Black);
            });
        });
    }

    private static void DrawTitle(
        ColumnDescriptor column, FormLayoutDto layout, FormDataDto data, float fontSize)
    {
        if (string.IsNullOrWhiteSpace(layout.Title))
        {
            return;
        }

        column.Item().PaddingTop(6).AlignCenter()
            .Text(Resolve(layout.Title, data).ToUpperInvariant())
            .Bold().FontSize(fontSize + 4);

        if (!string.IsNullOrWhiteSpace(layout.Subtitle))
        {
            column.Item().AlignCenter().Text(Resolve(layout.Subtitle, data)).Italic().FontSize(fontSize);
        }
    }

    private static void DrawIntro(ColumnDescriptor column, FormLayoutDto layout, FormDataDto data)
    {
        foreach (var line in layout.IntroLines)
        {
            var text = Resolve(line, data);

            if (!string.IsNullOrWhiteSpace(text))
            {
                column.Item().PaddingTop(2).Text(text);
            }
        }
    }

    private static void DrawFields(ColumnDescriptor column, FormLayoutDto layout, FormDataDto data)
    {
        if (layout.Fields.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(2).Column(fields =>
        {
            fields.Spacing(2);

            // Trường hẹp xếp hai cột cho gọn trang; trường rộng (lý do, nội dung) chiếm cả dòng vì
            // cắt đôi một câu dài làm tờ giấy khó đọc.
            var pending = new List<FormFieldDto>();

            void Flush()
            {
                if (pending.Count == 0)
                {
                    return;
                }

                var slice = pending.ToList();
                pending.Clear();

                fields.Item().Row(row =>
                {
                    foreach (var field in slice)
                    {
                        row.RelativeItem().Text(FieldText(field, data));
                    }

                    if (slice.Count == 1)
                    {
                        row.RelativeItem();
                    }
                });
            }

            foreach (var field in layout.Fields)
            {
                if (field.FullWidth)
                {
                    Flush();
                    fields.Item().Text(FieldText(field, data));
                    continue;
                }

                pending.Add(field);

                if (pending.Count == 2)
                {
                    Flush();
                }
            }

            Flush();
        });
    }

    private static string FieldText(FormFieldDto field, FormDataDto data)
    {
        var value = data.Fields.TryGetValue(field.Key, out var text) ? text : string.Empty;
        return string.IsNullOrWhiteSpace(field.Label) ? value : $"{field.Label}: {value}";
    }

    private static void DrawTable(
        ColumnDescriptor column, FormLayoutDto layout, FormDataDto data, float fontSize)
    {
        if (layout.Columns.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(definition =>
            {
                foreach (var col in layout.Columns)
                {
                    definition.RelativeColumn((float)Math.Max(0.2, col.Width));
                }
            });

            table.Header(header =>
            {
                foreach (var col in layout.Columns)
                {
                    header.Cell()
                        .Border(0.6f).BorderColor(Colors.Black)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(3)
                        .AlignCenter()
                        .Text(col.Header).Bold().FontSize(fontSize - 1);
                }
            });

            var index = 0;

            foreach (var row in data.Rows)
            {
                index++;

                foreach (var col in layout.Columns)
                {
                    var value = col.Key.Equals("index", StringComparison.OrdinalIgnoreCase)
                        ? index.ToString(CultureInfo.InvariantCulture)
                        : row.TryGetValue(col.Key, out var text) ? text : string.Empty;

                    var cell = table.Cell()
                        .Border(0.5f).BorderColor(Colors.Grey.Darken1)
                        .Padding(3);

                    cell = col.Align switch
                    {
                        "center" => cell.AlignCenter(),
                        "right" => cell.AlignRight(),
                        _ => cell
                    };

                    cell.Text(value).FontSize(fontSize - 1);
                }
            }

            if (!layout.ShowTotals || !layout.Columns.Any(col => col.Sum) || data.Rows.Count == 0)
            {
                return;
            }

            // Nhãn "Tổng cộng" đặt vào cột không cộng tổng đầu tiên đủ rộng để chứa nó; cột số thứ
            // tự thường chỉ rộng nửa phân nên chữ sẽ bị bẻ thành ba dòng nếu đặt ở đó.
            var labelIndex = layout.Columns.FindIndex(col => !col.Sum && col.Width >= 1.5);

            if (labelIndex < 0)
            {
                labelIndex = layout.Columns.FindIndex(col => !col.Sum);
            }

            for (var position = 0; position < layout.Columns.Count; position++)
            {
                var col = layout.Columns[position];

                var cell = table.Cell()
                    .Border(0.6f).BorderColor(Colors.Black)
                    .Background(Colors.Grey.Lighten4)
                    .Padding(3);

                if (col.Sum)
                {
                    var total = data.Rows.Sum(row => ParseNumber(row, col.Key));

                    cell.AlignRight().Text(total.ToString("#,##0", Vietnamese)).Bold().FontSize(fontSize - 1);
                }
                else if (position == labelIndex)
                {
                    cell.Text("Tổng cộng").Bold().FontSize(fontSize - 1);
                }
                else
                {
                    cell.Text(string.Empty);
                }
            }
        });
    }

    /// <summary>
    /// Đọc số từ ô đã định dạng để cộng tổng.
    ///
    /// Ô trong bảng đã là chuỗi hiển thị theo kiểu Việt Nam ("1.250.000"), nên dấu chấm là phân cách
    /// nghìn chứ không phải dấu thập phân — đọc nhầm chỗ này thì dòng tổng cộng sai cả nghìn lần.
    /// </summary>
    private static decimal ParseNumber(IReadOnlyDictionary<string, string> row, string key)
    {
        if (!row.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return decimal.TryParse(text, NumberStyles.Number, Vietnamese, out var value) ? value : 0;
    }

    private static void DrawClosing(ColumnDescriptor column, FormLayoutDto layout, FormDataDto data)
    {
        foreach (var line in layout.ClosingLines)
        {
            var text = Resolve(line, data);

            if (!string.IsNullOrWhiteSpace(text))
            {
                column.Item().PaddingTop(4).Text(text);
            }
        }
    }

    private static void DrawSignatures(
        ColumnDescriptor column, FormLayoutDto layout, FormDataDto data, float fontSize)
    {
        if (layout.Signatures.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(10).Row(row =>
        {
            foreach (var signature in layout.Signatures)
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().AlignCenter().Text(Resolve(signature.Role, data).ToUpperInvariant())
                        .Bold().FontSize(fontSize);

                    if (!string.IsNullOrWhiteSpace(signature.Note))
                    {
                        cell.Item().AlignCenter().Text(signature.Note).Italic().FontSize(fontSize - 1);
                    }

                    // Chừa chỗ ký tay: thiếu khoảng trắng này thì tờ giấy in ra không ký được.
                    cell.Item().Height(22, Unit.Millimetre);
                });
            }
        });
    }

    private static string Resolve(string template, FormDataDto data) =>
        Placeholder.Replace(template, match =>
            data.Fields.TryGetValue(match.Groups[1].Value, out var value) ? value : string.Empty);
}
