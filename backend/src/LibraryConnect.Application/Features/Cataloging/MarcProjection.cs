using System.Globalization;
using System.Text;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một tác giả rút được từ biểu ghi, kèm vai trò và biết là tác giả chính hay bổ sung.</summary>
public record ProjectedAuthor(string Name, string? Role, bool IsMain, bool IsCorporate, string? Dates);

/// <summary>Một chỉ số phân loại kèm tên bảng phân loại.</summary>
public record ProjectedClassification(string Code, string Scheme);

/// <summary>
/// Các giá trị rút từ biểu ghi MARC để đổ vào những cột phẳng của bảng biểu ghi.
/// </summary>
public class BibProjection
{
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string? StatementOfResponsibility { get; init; }
    public string? AuthorMain { get; init; }
    public string? UniformTitle { get; init; }
    public string? Isbn { get; init; }
    public string? Issn { get; init; }
    public string? PublisherName { get; init; }
    public string? PublishPlace { get; init; }
    public int? PublishYear { get; init; }
    public string? Edition { get; init; }
    public string? Pages { get; init; }
    public string? Dimensions { get; init; }
    public string? Ddc { get; init; }
    /// <summary>Mã ngôn ngữ ISO 639-2, ví dụ "vie".</summary>
    public string? LanguageCode { get; init; }
    /// <summary>Mã nước theo bảng mã MARC, ví dụ "vm".</summary>
    public string? CountryCode { get; init; }
    public string? SeriesTitle { get; init; }
    public string? SeriesVolume { get; init; }
    public string? Abstract { get; init; }

    public IReadOnlyList<ProjectedAuthor> Authors { get; init; } = Array.Empty<ProjectedAuthor>();
    public IReadOnlyList<string> Subjects { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ProjectedClassification> Classifications { get; init; } =
        Array.Empty<ProjectedClassification>();
}

/// <summary>
/// Rút các giá trị tra cứu được từ biểu ghi MARC.
///
/// The MARC record is the authoritative copy; these values are a projection kept beside it so that
/// list screens, sorting and OPAC facets do not have to reach into jsonb on every row. The
/// projection is rebuilt from the record on every save, never edited on its own — that is what keeps
/// the two from drifting apart.
///
/// Cataloguing rules put ISBD punctuation inside the MARC subfields ("Giáo trình cơ sở dữ liệu :"),
/// which belongs in the record but not in a column used for display and sorting, so it is trimmed
/// here and left untouched in the record itself.
/// </summary>
public static class MarcProjection
{
    /// <summary>
    /// Dấu câu ISBD ở cuối một trường con, cần bỏ khi đưa vào cột phẳng.
    ///
    /// The full stop is deliberately absent: in Vietnamese cataloguing it ends abbreviations that
    /// are part of the value itself — "356 tr.", "Nxb.", "tb." — and trimming it would corrupt them.
    /// </summary>
    private static readonly char[] IsbdTrailing = { ' ', '/', ':', ';', ',', '=', '+' };

    public static BibProjection Project(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        // In an RDA record 264 is repeated, one per function: the second indicator says which.
        // Publication is 1; the copyright date sits in 4 and must not be read as the year of
        // publication.
        var publication = record.GetFields("264").FirstOrDefault(field => field.Indicator2 == '1')
                          ?? record.GetField("260")
                          ?? record.GetFields("264").FirstOrDefault();

        var series = record.GetField("490") ?? record.GetField("830") ?? record.GetField("440");
        var fixedField = record.GetControlField("008") ?? string.Empty;

        return new BibProjection
        {
            Title = BuildTitle(record),
            Subtitle = Clean(record.GetSubfield("245", 'b')),
            StatementOfResponsibility = Clean(record.GetSubfield("245", 'c')),
            AuthorMain = MainAuthor(record),
            UniformTitle = Clean(record.GetSubfield("130", 'a') ?? record.GetSubfield("240", 'a')),
            Isbn = NormaliseStandardNumber(record.GetSubfield("020", 'a')),
            Issn = NormaliseStandardNumber(record.GetSubfield("022", 'a')),
            PublisherName = Clean(publication?.GetSubfield('b')),
            PublishPlace = Clean(publication?.GetSubfield('a')),
            PublishYear = ParseYear(publication?.GetSubfield('c')) ?? FixedFieldYear(fixedField),
            Edition = Clean(record.GetSubfield("250", 'a')),
            Pages = MoTaVatLy(record.GetSubfield("300", 'a')),
            Dimensions = Clean(record.GetSubfield("300", 'c')),
            Ddc = Clean(record.GetSubfield("082", 'a')),
            LanguageCode = Lower(record.GetSubfield("041", 'a')) ?? FixedFieldSlice(fixedField, 35, 3),
            CountryCode = Lower(record.GetSubfield("044", 'a')) ?? FixedFieldSlice(fixedField, 15, 3),
            SeriesTitle = Clean(series?.GetSubfield('a')),
            SeriesVolume = Clean(series?.GetSubfield('v')),
            Abstract = Clean(record.GetSubfield("520", 'a')),
            Authors = Authors(record),
            Subjects = Subjects(record),
            Keywords = Keywords(record),
            Classifications = Classifications(record)
        };
    }

    /// <summary>
    /// Nhan đề hiển thị: 245$a, nối thêm $n và $p nếu biểu ghi là một tập hoặc một phần của bộ.
    /// </summary>
    private static string BuildTitle(MarcRecord record)
    {
        var field = record.GetField("245");

        if (field is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Clean(field.GetSubfield('a')) ?? string.Empty);

        foreach (var subfield in field.Subfields.Where(item => item.Code is 'n' or 'p'))
        {
            var part = Clean(subfield.Value);

            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                // The part number follows the title after a full stop; the title often already ends
                // with one, and two in a row read as a typo on a catalogue card.
                builder.Append(builder[^1] == '.' ? " " : ". ");
            }

            builder.Append(part);
        }

        return builder.ToString();
    }

    private static string? MainAuthor(MarcRecord record) =>
        Clean(record.GetSubfield("100", 'a')
              ?? record.GetSubfield("110", 'a')
              ?? record.GetSubfield("111", 'a'));

    private static IReadOnlyList<ProjectedAuthor> Authors(MarcRecord record)
    {
        var authors = new List<ProjectedAuthor>();

        Add("100", isMain: true, corporate: false);
        Add("110", isMain: true, corporate: true);
        Add("111", isMain: true, corporate: true);
        Add("700", isMain: false, corporate: false);
        Add("710", isMain: false, corporate: true);
        Add("711", isMain: false, corporate: true);

        // The same person can appear as both 100 and 700 in records received from elsewhere; the
        // link table holds one row per person, so the main entry wins.
        return authors
            .GroupBy(author => VietnameseText.NormaliseForComparison(author.Name))
            .Select(group => group.OrderByDescending(author => author.IsMain).First())
            .ToList();

        void Add(string tag, bool isMain, bool corporate)
        {
            foreach (var field in record.GetFields(tag))
            {
                var name = Clean(field.GetSubfield('a'));

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                authors.Add(new ProjectedAuthor(
                    name,
                    Clean(field.GetSubfield('e')),
                    isMain,
                    corporate,
                    Clean(field.GetSubfield('d'))));
            }
        }
    }

    /// <summary>
    /// Đề mục chủ đề: nối $a với các phụ đề $v, $x, $y, $z bằng " -- " theo đúng cách trình bày
    /// quen thuộc của phích chủ đề.
    /// </summary>
    private static IReadOnlyList<string> Subjects(MarcRecord record)
    {
        var subjects = new List<string>();

        foreach (var tag in new[] { "600", "610", "611", "630", "650", "651" })
        {
            foreach (var field in record.GetFields(tag))
            {
                var parts = field.Subfields
                    .Where(subfield => subfield.Code is 'a' or 'v' or 'x' or 'y' or 'z')
                    .Select(subfield => Clean(subfield.Value))
                    .Where(value => !string.IsNullOrEmpty(value))
                    .ToList();

                if (parts.Count > 0)
                {
                    subjects.Add(string.Join(" -- ", parts));
                }
            }
        }

        return Distinct(subjects);
    }

    private static IReadOnlyList<string> Keywords(MarcRecord record) =>
        Distinct(record.GetFields("653")
            .SelectMany(field => field.GetSubfields('a'))
            .Select(Clean)
            .Where(value => !string.IsNullOrEmpty(value))
            .Select(value => value!)
            .ToList());

    private static IReadOnlyList<ProjectedClassification> Classifications(MarcRecord record)
    {
        var result = new List<ProjectedClassification>();

        foreach (var field in record.GetFields("082"))
        {
            foreach (var code in field.GetSubfields('a').Select(Clean).Where(value => !string.IsNullOrEmpty(value)))
            {
                result.Add(new ProjectedClassification(code!, "DDC"));
            }
        }

        foreach (var field in record.GetFields("080"))
        {
            var code = Clean(field.GetSubfield('a'));

            if (!string.IsNullOrEmpty(code))
            {
                result.Add(new ProjectedClassification(code, "UDC"));
            }
        }

        foreach (var field in record.GetFields("084"))
        {
            var code = Clean(field.GetSubfield('a'));

            if (!string.IsNullOrEmpty(code))
            {
                // $2 names the scheme; a library using its own table writes its name there.
                result.Add(new ProjectedClassification(code, Clean(field.GetSubfield('2')) ?? "Khác"));
            }
        }

        return result
            .GroupBy(item => $"{item.Scheme}|{item.Code}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>Bỏ khoảng trắng thừa và dấu câu ISBD ở cuối chuỗi.</summary>
    /// <summary>
    /// Mô tả vật lý, sau khi loại kiểu tệp.
    ///
    /// Trường 300 lẽ ra chứa "xii, 260 tr. : minh hoạ ; 24 cm", nhưng biểu ghi thu hoạch từ kho số
    /// lại hay mang `application/pdf` ở đó — kho nguồn khai `dc:format` là kiểu MIME, và bộ ánh xạ
    /// cũ đổ thẳng sang. Cột phẳng này chính là thứ trang tra cứu công khai đọc để hiện dòng "Mô tả
    /// vật lý", nên bạn đọc mở sách ra thấy "Mô tả vật lý: application/pdf".
    ///
    /// Chặn ở đây chứ không ở riêng bộ ánh xạ Dublin Core: biểu ghi còn vào kho qua ISO 2709, qua
    /// Z39.50, qua Excel và qua trình soạn MARC gõ tay — mọi lối ấy đều đi qua phép chiếu này.
    ///
    /// Nhận ra kiểu tệp bằng hình dạng: có dấu gạch chéo mà **không có khoảng trắng** nào. Mô tả
    /// vật lý thật luôn có khoảng trắng ("2 tập / 480 tr."), còn kiểu MIME thì không bao giờ có.
    ///
    /// Thà bỏ trống còn hơn hiện một chuỗi vô nghĩa: kiểu tệp đã có chỗ đứng đúng của nó ở `856$q`.
    /// </summary>
    public static string? MoTaVatLy(string? value)
    {
        var cleaned = Clean(value);

        if (cleaned is null)
        {
            return null;
        }

        return cleaned.Contains('/') && !cleaned.Any(char.IsWhiteSpace) ? null : cleaned;
    }

    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().TrimEnd(IsbdTrailing).Trim();

        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>
    /// Chuẩn hóa ISBN và ISSN để tra trùng: bỏ dấu gạch nối, khoảng trắng và phần chú thích trong
    /// ngoặc, giữ lại chữ số và ký tự kiểm tra X.
    /// </summary>
    public static string? NormaliseStandardNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsAsciiDigit(character))
            {
                builder.Append(character);
            }
            else if (character is 'x' or 'X')
            {
                builder.Append('X');
            }
            else if (character == '(')
            {
                // "9786040123456 (bìa mềm)" — everything from the bracket on is a qualifier.
                break;
            }
        }

        var result = builder.ToString();

        return result.Length == 0 ? null : result;
    }

    /// <summary>
    /// Rút năm xuất bản từ chuỗi tự do như "2023", "c2023", "[2023]", "2023, tái bản 2024".
    /// Lấy số bốn chữ số đầu tiên nằm trong khoảng hợp lý.
    /// </summary>
    public static int? ParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        for (var index = 0; index + 4 <= value.Length; index++)
        {
            var slice = value.AsSpan(index, 4);

            if (!slice.IsEmpty && char.IsAsciiDigit(slice[0]) && int.TryParse(slice, out var year)
                && year is >= 1400 and <= 2200)
            {
                return year;
            }
        }

        return null;
    }

    /// <summary>Năm xuất bản mã hóa ở trường 008 vị trí 07–10, dùng khi 260$c không đọc được.</summary>
    private static int? FixedFieldYear(string fixedField)
    {
        var slice = FixedFieldSlice(fixedField, 7, 4);

        return slice is not null && int.TryParse(slice, NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            && year is >= 1400 and <= 2200
            ? year
            : null;
    }

    /// <summary>Một đoạn của trường 008; trả về null nếu đoạn đó để trống hoặc trường quá ngắn.</summary>
    private static string? FixedFieldSlice(string fixedField, int start, int length)
    {
        if (fixedField.Length < start + length)
        {
            return null;
        }

        var slice = fixedField.Substring(start, length).Trim();

        // A run of "|" means the cataloguer made no attempt to code the position.
        return slice.Length == 0 || slice.All(character => character == '|') ? null : slice;
    }

    private static string? Lower(string? value) => Clean(value)?.ToLowerInvariant();

    private static IReadOnlyList<string> Distinct(List<string> values) =>
        values
            .GroupBy(VietnameseText.NormaliseForComparison)
            .Select(group => group.First())
            .ToList();
}
