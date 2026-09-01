using System.Xml.Linq;

namespace LibraryConnect.Marc.Oai;

/// <summary>
/// Chuyển đổi giữa MARC 21 và Dublin Core đơn giản (mục 3.4).
///
/// Dublin Core chỉ có 15 phần tử, nghèo hơn MARC rất nhiều, nên chiều MARC → DC là chiều mất mát:
/// giữ được nhan đề, tác giả, nhà xuất bản, năm, chủ đề, mô tả, định danh, ngôn ngữ. Chiều ngược
/// lại dựng ra một biểu ghi MARC tối thiểu nhưng hợp lệ — đủ để biên mục viên hiệu đính tiếp chứ
/// không phải bản thay thế cho biên mục thật.
/// </summary>
public static class DublinCore
{
    public static readonly XNamespace OaiDc = "http://www.openarchives.org/OAI/2.0/oai_dc/";
    public static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    public static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>Dựng phần tử <c>oai_dc:dc</c> từ một biểu ghi MARC.</summary>
    public static XElement FromMarc(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var element = new XElement(OaiDc + "dc",
            new XAttribute(XNamespace.Xmlns + "oai_dc", OaiDc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "dc", Dc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XAttribute(Xsi + "schemaLocation",
                "http://www.openarchives.org/OAI/2.0/oai_dc/ "
                + "http://www.openarchives.org/OAI/2.0/oai_dc.xsd"));

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                element.Add(new XElement(Dc + name, value.Trim(' ', '/', ':', ';', ',')));
            }
        }

        // 245$a$b là nhan đề; $c là thông tin trách nhiệm nên thuộc về dc:contributor chứ không
        // phải dc:title.
        var title = record.GetSubfield("245", 'a');
        var subtitle = record.GetSubfield("245", 'b');

        Add("title", string.IsNullOrWhiteSpace(subtitle)
            ? title
            : $"{title?.TrimEnd(' ', ':', '/')} : {subtitle}");

        Add("creator", record.GetSubfield("100", 'a') ?? record.GetSubfield("110", 'a'));

        foreach (var author in record.GetSubfields("700", 'a'))
        {
            Add("contributor", author);
        }

        foreach (var subject in record.GetSubfields("650", 'a').Concat(record.GetSubfields("653", 'a')))
        {
            Add("subject", subject);
        }

        Add("description", record.GetSubfield("520", 'a') ?? record.GetSubfield("500", 'a'));
        Add("publisher", record.GetSubfield("260", 'b') ?? record.GetSubfield("264", 'b'));
        Add("date", record.GetSubfield("260", 'c') ?? record.GetSubfield("264", 'c'));
        Add("type", TypeOf(record));
        Add("format", record.GetSubfield("300", 'a'));
        Add("identifier", record.GetSubfield("020", 'a') ?? record.ControlNumber);
        Add("source", record.GetSubfield("773", 't'));
        Add("language", record.GetSubfield("041", 'a') ?? LanguageFrom008(record));
        Add("relation", record.GetSubfield("490", 'a'));
        Add("rights", record.GetSubfield("540", 'a'));

        return element;
    }

    /// <summary>Dựng một biểu ghi MARC tối thiểu từ phần tử Dublin Core.</summary>
    public static MarcRecord ToMarc(XElement dc)
    {
        ArgumentNullException.ThrowIfNull(dc);

        var record = new MarcRecord();

        string? First(string name) => dc.Elements(Dc + name)
            .Select(element => ChuanHoaKhoangTrang(element.Value))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        IEnumerable<string> All(string name) => dc.Elements(Dc + name)
            .Select(element => ChuanHoaKhoangTrang(element.Value))
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var title = First("title") ?? "(Không có nhan đề)";
        var creator = First("creator");
        var publisher = First("publisher");
        var date = NamXuatBan(All("date"));

        // Kho nguồn có khai ngày nhưng không khai nổi một năm xuất bản nào thì vẫn phải mở trường
        // 264 để ghi "[không rõ]" vào đó - im lặng bỏ qua là để người đọc tưởng chưa ai điền.
        var coKhaiNgay = All("date").Any();
        var language = First("language");
        var identifier = First("identifier");

        // Leader: biểu ghi mới, ngôn ngữ tiếng Anh cấp mô tả đầy đủ — nguồn harvest chưa qua biên
        // mục nên đánh dấu mức mô tả là chưa đầy đủ để cán bộ biết còn phải hiệu đính.
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.Leader.EncodingLevel = '3';
        record.Leader.DescriptiveCatalogingForm = 'i';

        var titleField = record.AddField("245", '1', '0');
        titleField.AddSubfield('a', title);

        if (creator is not null)
        {
            titleField.AddSubfield('c', creator);
            record.AddField("100", '1').AddSubfield('a', creator);
        }

        if (publisher is not null || coKhaiNgay)
        {
            var publication = record.AddField("264", ' ', '1');

            if (publisher is not null)
            {
                publication.AddSubfield('b', publisher);
            }

            // Luôn ghi $c: "[không rõ]" là câu trả lời thật, còn bỏ trống thì người đọc biểu ghi
            // không phân biệt được "chưa ai điền" với "đã tra mà không ra".
            publication.AddSubfield('c', date ?? KhongRoNam);
        }

        foreach (var subject in All("subject"))
        {
            record.AddField("653").AddSubfield('a', subject);
        }

        var description = First("description");

        if (description is not null)
        {
            record.AddField("520").AddSubfield('a', description);
        }

        var format = First("format");
        var mime = format is not null && LaKieuTep(format) ? format : null;

        // "application/pdf" là kiểu tệp, không phải mô tả vật lý. Không biết số trang thì bỏ trống
        // cả trường 300 - bịa một giá trị vào đó còn tệ hơn để trống.
        if (format is not null && mime is null)
        {
            record.AddField("300").AddSubfield('a', format);
        }

        // Ngôn ngữ luôn ghi, kể cả khi kho nguồn không khai: "und" là mã chuẩn cho "không xác
        // định", và trường 008 vị trí 35-37 phải khớp với chỗ này.
        record.AddField("041").AddSubfield('a', LanguageCodes.ToMarc21(language));

        if (identifier is not null)
        {
            // ISBN thật thì vào 020, còn lại là địa chỉ hoặc mã nội bộ nên vào 856 hoặc 024.
            if (LooksLikeIsbn(identifier))
            {
                record.AddField("020").AddSubfield('a', identifier);
            }
            else if (identifier.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var dienTu = record.AddField("856", '4', '0');
                dienTu.AddSubfield('u', identifier);

                // $q là chỗ của kiểu tệp theo MARC 21 - thứ trước đây bị đổ nhầm vào trường 300.
                if (mime is not null)
                {
                    dienTu.AddSubfield('q', mime);
                }
            }
            else
            {
                record.AddField("024", '8').AddSubfield('a', identifier);
            }
        }

        foreach (var contributor in All("contributor"))
        {
            record.AddField("700", '1').AddSubfield('a', contributor);
        }

        // Kiểu tệp còn sót lại mà chưa có 856 nào để gắn thì tự mở một trường 856 cho nó.
        if (mime is not null && record.GetField("856") is null)
        {
            record.AddField("856", '4', '0').AddSubfield('q', mime);
        }

        ThemBoBaRda(record, dienTu: record.GetField("856") is not null);

        return record;
    }

    /// <summary>Ghi vào 264$c khi kho nguồn không cho biết năm xuất bản thật.</summary>
    public const string KhongRoNam = "[không rõ]";

    /// <summary>
    /// Gộp mọi khoảng trắng thành một dấu cách, bỏ ký tự xuống dòng.
    ///
    /// Kho nguồn hay để nguyên ký tự xuống dòng của biểu mẫu nhập liệu trong nhan đề. Một trường
    /// MARC là một dòng chữ liền, không có khái niệm xuống dòng, nên ký tự ấy chỉ gây rắc rối: nhan
    /// đề hiện vỡ trên giao diện, phép so trùng theo nhan đề trượt, và cùng một biểu ghi xuất ra
    /// ISO 2709 với MARCXML lại khác nhau — chuẩn XML bắt bộ đọc đổi CR LF thành LF, còn ISO 2709
    /// giữ nguyên byte.
    /// </summary>
    private static string ChuanHoaKhoangTrang(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        var khoangTrang = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                khoangTrang = true;
                continue;
            }

            if (khoangTrang && builder.Length > 0)
            {
                builder.Append(' ');
            }

            khoangTrang = false;
            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Bộ ba RDA 336/337/338 - loại nội dung, phương tiện và vật mang tin.
    ///
    /// Đây là phần bắt buộc của biểu ghi biên mục theo RDA, thay chỗ của trường 245$h ngày xưa.
    /// Dublin Core không có thông tin này, nhưng suy ra được: mọi thứ thu hoạch về đây đều là chữ
    /// (Leader/06 = 'a'), chỉ khác nhau ở chỗ đọc trên máy hay cầm trên tay.
    /// </summary>
    private static void ThemBoBaRda(MarcRecord record, bool dienTu)
    {
        void Them(string tag, string a, string b, string nguon)
        {
            if (record.GetField(tag) is not null)
            {
                return;
            }

            var field = record.AddField(tag);
            field.AddSubfield('a', a);
            field.AddSubfield('b', b);
            field.AddSubfield('2', nguon);
        }

        Them("336", "text", "txt", "rdacontent");
        Them("337", dienTu ? "computer" : "unmediated", dienTu ? "c" : "n", "rdamedia");
        Them("338", dienTu ? "online resource" : "volume", dienTu ? "cr" : "nc", "rdacarrier");
    }

    /// <summary>
    /// Chọn năm xuất bản trong danh sách dc:date, bỏ qua dấu thời gian của kho nguồn.
    ///
    /// DSpace gộp mọi dc.date.* vào cùng một danh sách: dc.date.accessioned là lúc kho nhận bản ghi
    /// (2020-02-17T09:38:18Z), còn dc.date.issued mới là năm tài liệu ra đời. Lấy bừa phần tử đầu
    /// tiên thì 7.464 trên 7.466 biểu ghi thu về mang năm xuất bản là ngày kho nguồn nhập liệu -
    /// sai với gần như mọi biểu ghi, và sai theo cách trông vẫn hợp lệ.
    /// </summary>
    private static string? NamXuatBan(IEnumerable<string> dates)
    {
        foreach (var value in dates)
        {
            var text = value.Trim();

            // Có phần giờ thì là dấu thời gian của hệ thống, không phải ngày xuất bản do người biên
            // mục ghi.
            if (text.Contains('T') && text.Contains(':'))
            {
                continue;
            }

            if (BonChuSoNam(text) is { } nam)
            {
                return nam;
            }
        }

        return null;
    }

    /// <summary>Bốn chữ số năm ở đầu chuỗi, nếu chuỗi ấy là một ngày tháng thật.</summary>
    private static string? BonChuSoNam(string text)
    {
        if (text.Length < 4)
        {
            return null;
        }

        var dau = text[..4];

        if (!dau.All(char.IsAsciiDigit))
        {
            return null;
        }

        // Năm ngoài khoảng này gần như chắc chắn là số hiệu chứ không phải năm xuất bản.
        return int.TryParse(dau, out var nam) && nam is >= 1000 and <= 2999 ? dau : null;
    }

    /// <summary>
    /// Chuỗi này là kiểu tệp (MIME) hay mô tả vật lý.
    ///
    /// Kiểu tệp có dạng nhom/loai và không có khoảng trắng - application/pdf, text/html, video/mp4.
    /// Mô tả vật lý thì luôn có chữ và số trang: "215 tr. ; 24 cm".
    /// </summary>
    private static bool LaKieuTep(string value)
    {
        var text = value.Trim();

        return text.Count(character => character == '/') == 1
               && !text.Contains(' ')
               && text.Length is > 3 and < 100
               && text[0] != '/'
               && text[^1] != '/';
    }

    private static string? TypeOf(MarcRecord record) => record.Leader.RecordType switch
    {
        'a' => "text",
        'c' or 'd' => "notated music",
        'e' or 'f' => "cartographic",
        'g' => "moving image",
        'i' or 'j' => "sound",
        'k' => "still image",
        'm' => "software",
        _ => "text",
    };

    private static string? LanguageFrom008(MarcRecord record)
    {
        var field = record.GetControlField("008");

        // Vị trí 35–37 của trường 008 là mã ngôn ngữ ISO 639-2.
        return field is { Length: >= 38 } ? field.Substring(35, 3).Trim() : null;
    }

    private static string NormalizeLanguage(string language)
    {
        var value = language.Trim().ToLowerInvariant();

        return value switch
        {
            "vi" or "vie" or "vietnamese" or "tiếng việt" => "vie",
            "en" or "eng" or "english" => "eng",
            "fr" or "fre" or "fra" or "french" => "fre",
            _ => value.Length >= 3 ? value[..3] : value,
        };
    }

    private static bool LooksLikeIsbn(string value)
    {
        var digits = value.Where(char.IsAsciiDigit).Count();
        var hasLetters = value.Any(character => char.IsLetter(character) && character != 'X' && character != 'x');

        return !hasLetters && digits is 10 or 13;
    }
}
