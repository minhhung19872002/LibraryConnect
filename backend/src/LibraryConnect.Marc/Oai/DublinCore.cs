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
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        IEnumerable<string> All(string name) => dc.Elements(Dc + name)
            .Select(element => element.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var title = First("title") ?? "(Không có nhan đề)";
        var creator = First("creator");
        var publisher = First("publisher");
        var date = First("date");
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

        if (publisher is not null || date is not null)
        {
            var publication = record.AddField("264", ' ', '1');

            if (publisher is not null)
            {
                publication.AddSubfield('b', publisher);
            }

            if (date is not null)
            {
                publication.AddSubfield('c', date);
            }
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

        if (format is not null)
        {
            record.AddField("300").AddSubfield('a', format);
        }

        if (language is not null)
        {
            record.AddField("041").AddSubfield('a', NormalizeLanguage(language));
        }

        if (identifier is not null)
        {
            // ISBN thật thì vào 020, còn lại là địa chỉ hoặc mã nội bộ nên vào 856 hoặc 024.
            if (LooksLikeIsbn(identifier))
            {
                record.AddField("020").AddSubfield('a', identifier);
            }
            else if (identifier.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                record.AddField("856", '4', '0').AddSubfield('u', identifier);
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

        return record;
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
