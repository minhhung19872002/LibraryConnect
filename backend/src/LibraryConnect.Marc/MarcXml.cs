using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LibraryConnect.Marc;

/// <summary>
/// Đọc và ghi MARCXML theo lược đồ <c>http://www.loc.gov/MARC21/slim</c>.
///
/// This is the form MARC takes in SRU responses and in OAI-PMH records, so both the interoperability
/// endpoints and the file import/export screens go through here.
///
/// In XML the record has no byte offsets, so the two numeric areas of the leader are written as
/// zeros. A reader that needs them recomputes them when it writes ISO 2709, which is exactly what
/// <see cref="Iso2709Writer"/> does.
/// </summary>
public static class MarcXml
{
    /// <summary>Không gian tên của lược đồ MARCXML.</summary>
    public static readonly XNamespace Namespace = "http://www.loc.gov/MARC21/slim";

    public const string SchemaLocation =
        "http://www.loc.gov/MARC21/slim http://www.loc.gov/standards/marcxml/schema/MARC21slim.xsd";

    /// <summary>Chuyển một biểu ghi thành phần tử <c>record</c>.</summary>
    public static XElement ToXml(MarcRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var leader = record.Leader.Clone();
        // Lengths belong to the exchange format, not to XML; zeros are what LoC writes here too.
        leader.SetRecordLength(0);
        leader.SetBaseAddressOfData(0);

        var element = new XElement(Namespace + "record", new XElement(Namespace + "leader", leader.ToString()));

        foreach (var field in record.ControlFields)
        {
            element.Add(new XElement(
                Namespace + "controlfield",
                new XAttribute("tag", field.Tag),
                Sanitise(field.Value)));
        }

        foreach (var field in record.DataFields)
        {
            var dataField = new XElement(
                Namespace + "datafield",
                new XAttribute("tag", field.Tag),
                new XAttribute("ind1", field.Indicator1.ToString()),
                new XAttribute("ind2", field.Indicator2.ToString()));

            foreach (var subfield in field.Subfields)
            {
                dataField.Add(new XElement(
                    Namespace + "subfield",
                    new XAttribute("code", subfield.Code.ToString()),
                    Sanitise(subfield.Value)));
            }

            element.Add(dataField);
        }

        return element;
    }

    /// <summary>Chuyển nhiều biểu ghi thành phần tử <c>collection</c>.</summary>
    public static XElement ToCollectionXml(IEnumerable<MarcRecord> records)
    {
        var collection = new XElement(Namespace + "collection");

        foreach (var record in records)
        {
            collection.Add(ToXml(record));
        }

        return collection;
    }

    /// <summary>Ghi một tệp MARCXML hoàn chỉnh, mã hóa UTF-8, có khai báo XML.</summary>
    public static byte[] WriteCollection(IEnumerable<MarcRecord> records)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            ToCollectionXml(records));

        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  "
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }

    public static string WriteRecord(MarcRecord record) => ToXml(record).ToString();

    /// <summary>Đọc một phần tử <c>record</c> thành biểu ghi.</summary>
    public static MarcRecord FromXml(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element.Name.LocalName != "record")
        {
            throw new MarcException(
                $"Phần tử \"{element.Name.LocalName}\" không phải biểu ghi MARCXML. Mong đợi phần tử \"record\".");
        }

        var record = new MarcRecord();
        var leader = element.Elements().FirstOrDefault(child => child.Name.LocalName == "leader");

        if (leader is not null)
        {
            record.Leader = new MarcLeader(leader.Value);
        }

        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "controlfield":
                    record.ControlFields.Add(new MarcControlField(ReadTag(child), child.Value));
                    break;

                case "datafield":
                    record.DataFields.Add(ReadDataField(child));
                    break;
            }
        }

        return record;
    }

    /// <summary>
    /// Đọc mọi biểu ghi trong một tài liệu MARCXML. Chấp nhận cả tài liệu chỉ có một
    /// <c>record</c> lẫn tài liệu bọc trong <c>collection</c>, và cả biểu ghi nằm lồng trong
    /// phản hồi SRU hoặc OAI-PMH.
    /// </summary>
    public static IReadOnlyList<MarcRecord> ReadAll(string xml)
    {
        XDocument document;

        try
        {
            document = XDocument.Parse(xml, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new MarcException($"Tệp XML không hợp lệ: {exception.Message}", exception);
        }

        if (document.Root is null)
        {
            throw new MarcException("Tệp XML rỗng.");
        }

        var records = document.Root.Name.LocalName == "record"
            ? new[] { document.Root }
            : document.Descendants().Where(element => element.Name.LocalName == "record").ToArray();

        if (records.Length == 0)
        {
            throw new MarcException(
                "Không tìm thấy biểu ghi nào trong tệp. Tệp MARCXML phải chứa phần tử \"record\" theo " +
                "lược đồ http://www.loc.gov/MARC21/slim.");
        }

        return records.Select(FromXml).ToList();
    }

    public static IReadOnlyList<MarcRecord> ReadAll(byte[] data) =>
        ReadAll(Encoding.UTF8.GetString(TrimByteOrderMark(data)));

    private static MarcDataField ReadDataField(XElement element)
    {
        var field = new MarcDataField(
            ReadTag(element),
            ReadIndicator(element, "ind1"),
            ReadIndicator(element, "ind2"));

        foreach (var subfield in element.Elements().Where(child => child.Name.LocalName == "subfield"))
        {
            var code = subfield.Attribute("code")?.Value;

            if (string.IsNullOrEmpty(code))
            {
                throw new MarcException(
                    $"Trường {field.Tag} có trường con thiếu thuộc tính \"code\".");
            }

            field.Subfields.Add(new MarcSubfield(code[0], subfield.Value));
        }

        return field;
    }

    private static string ReadTag(XElement element)
    {
        var tag = element.Attribute("tag")?.Value;

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new MarcException(
                $"Phần tử \"{element.Name.LocalName}\" thiếu thuộc tính \"tag\".");
        }

        // Some exporters write 245 as "245" but others as "0245" or " 45"; normalise to three digits.
        tag = tag.Trim().PadLeft(MarcConstants.TagLength, '0');

        return tag.Length > MarcConstants.TagLength ? tag[^MarcConstants.TagLength..] : tag;
    }

    /// <summary>An absent or empty indicator means "not defined", which MARC writes as a blank.</summary>
    private static char ReadIndicator(XElement element, string name)
    {
        var value = element.Attribute(name)?.Value;

        return string.IsNullOrEmpty(value) ? ' ' : value[0];
    }

    /// <summary>
    /// Bỏ các ký tự điều khiển mà XML 1.0 không cho phép.
    ///
    /// Records converted from older systems sometimes carry a stray 0x1F or a tab-like control
    /// character inside a value. Writing it would produce a file no XML parser accepts, so it is
    /// dropped here rather than breaking the whole export.
    /// </summary>
    private static string Sanitise(string value)
    {
        if (!value.Any(IsForbidden))
        {
            return value;
        }

        return new string(value.Where(character => !IsForbidden(character)).ToArray());
    }

    private static bool IsForbidden(char character) =>
        character < 0x20 && character is not ('\t' or '\n' or '\r');

    private static byte[] TrimByteOrderMark(byte[] data) =>
        data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF
            ? data[3..]
            : data;
}
