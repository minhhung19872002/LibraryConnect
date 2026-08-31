using System.Text;
using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một vùng mô tả ISBD, dùng cho tab "Thông tin thư mục" và cho việc in phích.</summary>
public record IsbdArea(string Label, string Content);

/// <summary>
/// Trình bày biểu ghi MARC theo mô tả thư mục ISBD.
///
/// A cataloguer proofreads a record by reading it as a bibliographic description, not as a table of
/// tags, and the same description is what goes on a catalogue card. The punctuation is generated
/// here rather than taken from the subfields: records arriving from other systems are inconsistent
/// about whether they carry it, and a description with doubled or missing marks looks wrong on a
/// printed card.
/// </summary>
public static class IsbdFormatter
{
    /// <summary>Mô tả đầy đủ, mỗi vùng một dòng.</summary>
    public static IReadOnlyList<IsbdArea> Describe(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var projection = MarcProjection.Project(record);
        var areas = new List<IsbdArea>();

        Add("Nhan đề và thông tin trách nhiệm", TitleArea(projection));
        Add("Lần xuất bản", projection.Edition);
        Add("Thông tin xuất bản", PublicationArea(projection));
        Add("Mô tả vật lý", PhysicalArea(record, projection));
        Add("Tùng thư", SeriesArea(projection));
        Add("Phụ chú", NotesArea(record));
        Add("Chỉ số ISBN", StandardNumberArea(record, projection));
        Add("Chỉ số phân loại", ClassificationArea(projection));
        Add("Đề mục chủ đề", string.Join("; ", projection.Subjects));
        Add("Từ khóa", string.Join("; ", projection.Keywords));
        Add("Tóm tắt", projection.Abstract);

        return areas;

        void Add(string label, string? content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                areas.Add(new IsbdArea(label, content.Trim()));
            }
        }
    }

    /// <summary>Mô tả gộp thành một đoạn văn, các vùng ngăn nhau bằng dấu ". — " như trên phích.</summary>
    public static string DescribeAsParagraph(MarcRecord record) =>
        string.Join(". — ", Describe(record).Select(area => area.Content));

    private static string TitleArea(BibProjection projection)
    {
        var builder = new StringBuilder(projection.Title);

        if (!string.IsNullOrEmpty(projection.Subtitle))
        {
            builder.Append(" : ").Append(projection.Subtitle);
        }

        if (!string.IsNullOrEmpty(projection.StatementOfResponsibility))
        {
            builder.Append(" / ").Append(projection.StatementOfResponsibility);
        }

        return builder.ToString();
    }

    private static string PublicationArea(BibProjection projection)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(projection.PublishPlace))
        {
            parts.Add(projection.PublishPlace);
        }

        if (!string.IsNullOrEmpty(projection.PublisherName))
        {
            parts.Add(projection.PublisherName);
        }

        var head = string.Join(" : ", parts);

        return projection.PublishYear is null
            ? head
            : string.IsNullOrEmpty(head)
                ? projection.PublishYear.Value.ToString()
                : $"{head}, {projection.PublishYear}";
    }

    private static string PhysicalArea(MarcRecord record, BibProjection projection)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(projection.Pages))
        {
            parts.Add(projection.Pages);
        }

        var illustrations = MarcProjection.Clean(record.GetSubfield("300", 'b'));

        if (!string.IsNullOrEmpty(illustrations))
        {
            parts.Add($": {illustrations}");
        }

        if (!string.IsNullOrEmpty(projection.Dimensions))
        {
            parts.Add($"; {projection.Dimensions}");
        }

        var accompanying = MarcProjection.Clean(record.GetSubfield("300", 'e'));

        if (!string.IsNullOrEmpty(accompanying))
        {
            parts.Add($"+ {accompanying}");
        }

        return string.Join(" ", parts);
    }

    private static string SeriesArea(BibProjection projection)
    {
        if (string.IsNullOrEmpty(projection.SeriesTitle))
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(projection.SeriesVolume)
            ? $"({projection.SeriesTitle})"
            : $"({projection.SeriesTitle} ; {projection.SeriesVolume})";
    }

    /// <summary>Gom các trường phụ chú 500–599 mà cán bộ thường đọc khi soát biểu ghi.</summary>
    private static string NotesArea(MarcRecord record)
    {
        var notes = new[] { "500", "501", "502", "504", "505", "546" }
            .SelectMany(record.GetFields)
            .Select(field => MarcProjection.Clean(field.GetSubfield('a')))
            .Where(note => !string.IsNullOrEmpty(note));

        return string.Join(". ", notes);
    }

    private static string StandardNumberArea(MarcRecord record, BibProjection projection)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(projection.Isbn))
        {
            var price = MarcProjection.Clean(record.GetSubfield("020", 'c'));
            parts.Add(string.IsNullOrEmpty(price) ? projection.Isbn : $"{projection.Isbn} : {price}");
        }

        if (!string.IsNullOrEmpty(projection.Issn))
        {
            parts.Add($"ISSN {projection.Issn}");
        }

        return string.Join(". — ", parts);
    }

    private static string ClassificationArea(BibProjection projection) =>
        string.Join("; ", projection.Classifications.Select(item => $"{item.Code} ({item.Scheme})"));
}
