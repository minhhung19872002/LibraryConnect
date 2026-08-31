namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>Definition of one column of a printed table report.</summary>
public record PdfColumn<T>(string Header, Func<T, string> Value, float Width = 1f, PdfAlign Align = PdfAlign.Left);

public enum PdfAlign { Left, Center, Right }

/// <summary>
/// Everything the standard report header carries. Section 6.6 asks every printed output to look the
/// same, so the title block, the applied filters and the footer are produced centrally rather than
/// by each report.
/// </summary>
public class PdfReportHeader
{
    /// <summary>Customer's library name, read from the system parameters — never hardcoded.</summary>
    public string LibraryName { get; set; } = string.Empty;
    public string? LibraryAddress { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    /// <summary>Filter summary printed under the title, e.g. "Từ 01/01/2026 đến 31/01/2026".</summary>
    public IReadOnlyList<string> Criteria { get; set; } = Array.Empty<string>();
    public string? PreparedBy { get; set; }
    public bool Landscape { get; set; }
}

public interface IPdfReportService
{
    /// <summary>Renders a paginated table report in the shared house style.</summary>
    byte[] RenderTable<T>(PdfReportHeader header, IReadOnlyList<PdfColumn<T>> columns, IReadOnlyList<T> rows);
}
