using ClosedXML.Excel;
using LibraryConnect.Application.Common.Interfaces;

namespace LibraryConnect.Reporting.Excel;

/// <summary>
/// ClosedXML implementation of the shared Excel import/export surface.
///
/// Reading is deliberately forgiving: headers are matched case-insensitively after trimming, every
/// cell is read as a formatted string, and completely blank rows are skipped. Librarians edit these
/// files by hand, so a stray space must not fail an import of several thousand rows.
/// </summary>
public class ExcelService : IExcelService
{
    private const int MaxScannedRows = 200_000;

    public ExcelSheet Read(Stream stream, string? sheetName = null)
    {
        using var workbook = new XLWorkbook(stream);

        var worksheet = sheetName is null
            ? workbook.Worksheets.FirstOrDefault()
            : workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));

        if (worksheet is null)
        {
            throw new InvalidOperationException(
                sheetName is null
                    ? "Tệp Excel không có sheet nào."
                    : $"Không tìm thấy sheet '{sheetName}' trong tệp Excel.");
        }

        var used = worksheet.RangeUsed();
        if (used is null)
        {
            return new ExcelSheet { Name = worksheet.Name };
        }

        var firstRow = used.FirstRow();
        var headers = firstRow.Cells()
            .Select(cell => cell.GetFormattedString().Trim())
            .ToList();

        var rows = new List<ExcelRow>();

        foreach (var row in used.Rows().Skip(1).Take(MaxScannedRows))
        {
            var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                cells[header] = row.Cell(i + 1).GetFormattedString().Trim();
            }

            var excelRow = new ExcelRow { RowNumber = row.RowNumber(), Cells = cells };

            if (!excelRow.IsEmpty)
            {
                rows.Add(excelRow);
            }
        }

        return new ExcelSheet
        {
            Name = worksheet.Name,
            Headers = headers.Where(h => !string.IsNullOrWhiteSpace(h)).ToList(),
            Rows = rows
        };
    }

    public byte[] Write<T>(string sheetName, IReadOnlyList<ExcelColumn<T>> columns, IEnumerable<T> rows, string? title = null)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SafeSheetName(sheetName));

        var headerRow = 1;

        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleCell = sheet.Cell(1, 1);
            titleCell.Value = title;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            sheet.Range(1, 1, 1, Math.Max(columns.Count, 1)).Merge();
            sheet.Cell(2, 1).Value = $"Ngày xuất: {DateTimeOffset.Now:HH:mm dd/MM/yyyy}";
            sheet.Cell(2, 1).Style.Font.Italic = true;
            headerRow = 4;
        }

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = sheet.Cell(headerRow, i + 1);
            cell.Value = columns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        var rowIndex = headerRow + 1;

        foreach (var item in rows)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = sheet.Cell(rowIndex, i + 1);
                SetValue(cell, columns[i].Value(item));

                if (columns[i].Format is { } format)
                {
                    cell.Style.NumberFormat.Format = format;
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            rowIndex++;
        }

        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].Width is { } width)
            {
                sheet.Column(i + 1).Width = width;
            }
            else
            {
                sheet.Column(i + 1).AdjustToContents(headerRow, Math.Min(rowIndex, headerRow + 200));
                // AdjustToContents can produce very wide columns from a single long value.
                sheet.Column(i + 1).Width = Math.Clamp(sheet.Column(i + 1).Width, 10, 60);
            }
        }

        if (rowIndex > headerRow + 1)
        {
            sheet.Range(headerRow, 1, rowIndex - 1, columns.Count).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(headerRow);

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public byte[] WriteTemplate(
        string sheetName,
        IReadOnlyList<ExcelTemplateColumn> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>>? sampleRows = null)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SafeSheetName(sheetName));

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = columns[i].Header;
            cell.Style.Font.Bold = true;
            // Required columns are tinted so a user filling the file in sees them at a glance.
            cell.Style.Fill.BackgroundColor = columns[i].Required
                ? XLColor.FromHtml("#FFF1F0")
                : XLColor.FromHtml("#E8F0FE");
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.GetComment().AddText(columns[i].Description);
            sheet.Column(i + 1).Width = Math.Clamp(columns[i].Header.Length + 6, 14, 40);
        }

        var rowIndex = 2;
        foreach (var sample in sampleRows ?? Array.Empty<IReadOnlyDictionary<string, string>>())
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (sample.TryGetValue(columns[i].Header, out var value))
                {
                    sheet.Cell(rowIndex, i + 1).Value = value;
                }
            }

            rowIndex++;
        }

        sheet.SheetView.FreezeRows(1);

        // Second sheet: what each column means, which ones are mandatory and an example value.
        var guide = workbook.AddWorksheet("Hướng dẫn");
        guide.Cell(1, 1).Value = "Cột";
        guide.Cell(1, 2).Value = "Bắt buộc";
        guide.Cell(1, 3).Value = "Ý nghĩa";
        guide.Cell(1, 4).Value = "Ví dụ";
        guide.Range(1, 1, 1, 4).Style.Font.Bold = true;
        guide.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");

        for (var i = 0; i < columns.Count; i++)
        {
            guide.Cell(i + 2, 1).Value = columns[i].Header;
            guide.Cell(i + 2, 2).Value = columns[i].Required ? "Có" : "Không";
            guide.Cell(i + 2, 3).Value = columns[i].Description;
            guide.Cell(i + 2, 4).Value = columns[i].Example ?? string.Empty;
        }

        guide.Column(1).Width = 26;
        guide.Column(2).Width = 10;
        guide.Column(3).Width = 70;
        guide.Column(4).Width = 26;
        guide.Column(3).Style.Alignment.WrapText = true;

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static void SetValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case string text:
                cell.Value = text;
                break;
            case bool flag:
                cell.Value = flag ? "Có" : "Không";
                break;
            case DateTimeOffset timestamp:
                cell.Value = timestamp.LocalDateTime;
                cell.Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.NumberFormat.Format = "dd/MM/yyyy";
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = "dd/MM/yyyy";
                break;
            case decimal number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = "#,##0";
                break;
            case int or long or double:
                cell.Value = Convert.ToDouble(value);
                break;
            case Enum enumeration:
                cell.Value = enumeration.ToString();
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    /// <summary>Excel rejects sheet names over 31 characters or containing : \ / ? * [ ]</summary>
    private static string SafeSheetName(string name)
    {
        var cleaned = new string(name.Where(c => !":\\/?*[]".Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Sheet1";
        }

        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
