namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>One row read from an uploaded sheet, addressed by column header.</summary>
public class ExcelRow
{
    /// <summary>1-based row number in the sheet, so an error message can point the user at it.</summary>
    public int RowNumber { get; init; }

    public IReadOnlyDictionary<string, string> Cells { get; init; } = new Dictionary<string, string>();

    /// <summary>Trimmed value of a column, or an empty string when the column is absent or blank.</summary>
    public string Get(string column) => Cells.TryGetValue(column, out var value) ? value : string.Empty;

    public bool IsEmpty => Cells.Values.All(string.IsNullOrWhiteSpace);
}

public class ExcelSheet
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ExcelRow> Rows { get; init; } = Array.Empty<ExcelRow>();
}

/// <summary>Definition of one exported column.</summary>
public record ExcelColumn<T>(string Header, Func<T, object?> Value, double? Width = null, string? Format = null);

/// <summary>
/// Excel import and export (ClosedXML). Used by every "Nhập từ Excel" and "Xuất Excel" function in
/// the product, so the header handling and the error reporting stay consistent across modules.
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Reads the first sheet (or the named one) into header-addressed rows. Headers are trimmed and
    /// compared case-insensitively so a slightly edited template still imports.
    /// </summary>
    ExcelSheet Read(Stream stream, string? sheetName = null);

    /// <summary>
    /// Renders a table with a styled header row, auto-filter and frozen header.
    ///
    /// Passing a <paramref name="title"/> produces a report for a human reader: the title block takes
    /// the first rows and the header moves down, so such a file is not meant to be read back with
    /// <see cref="Read"/>. Omit the title for files intended for data exchange.
    /// </summary>
    byte[] Write<T>(string sheetName, IReadOnlyList<ExcelColumn<T>> columns, IEnumerable<T> rows, string? title = null);

    /// <summary>
    /// Builds an import template: the header row plus a second sheet explaining each column, which
    /// is what the "Tải file mẫu" buttons hand to the user.
    /// </summary>
    byte[] WriteTemplate(string sheetName, IReadOnlyList<ExcelTemplateColumn> columns, IReadOnlyList<IReadOnlyDictionary<string, string>>? sampleRows = null);
}

/// <summary>A column of an import template, with the guidance shown on the instructions sheet.</summary>
public record ExcelTemplateColumn(string Header, string Description, bool Required = false, string? Example = null);
