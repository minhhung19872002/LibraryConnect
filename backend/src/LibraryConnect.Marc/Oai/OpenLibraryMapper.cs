using System.Text.Json;

namespace LibraryConnect.Marc.Oai;

/// <summary>Một biểu ghi Open Library đã đọc ra khỏi JSON, trước khi dựng thành MARC.</summary>
public record OpenLibraryDoc(
    string Key,
    string Title,
    IReadOnlyList<string> Authors,
    int? FirstPublishYear,
    string? Isbn,
    long? CoverId,
    string? Publisher,
    IReadOnlyList<string> Subjects,
    string? Language,
    int? PageCount);

/// <summary>
/// Dựng biểu ghi MARC 21 từ dữ liệu Open Library.
///
/// Open Library công bố dữ liệu thư mục theo giấy phép **CC0** — hiến tặng vào phạm vi công cộng,
/// dùng lại không cần xin phép và không phải ghi nguồn (nhưng vẫn ghi, ở trường 035 và 040). Ảnh bìa
/// lấy theo mã ảnh (`cover_i`) chứ không theo ISBN: lối theo mã ảnh không bị giới hạn tần suất, còn
/// lối theo ISBN thì bị chặn 100 lượt mỗi 5 phút cho một địa chỉ.
///
/// Dữ liệu của họ đầy đủ hơn Dublin Core rất nhiều — có nhan đề, tác giả, năm, ISBN, nhà xuất bản,
/// đề mục chủ đề, ngôn ngữ, số trang — nên biểu ghi dựng ra đủ chuẩn để công bố ngay, không phải qua
/// hàng đợi hiệu đính như biểu ghi thu hoạch bằng OAI-PMH.
/// </summary>
public static class OpenLibraryMapper
{
    /// <summary>Đọc một phần tử `docs` của Open Library Search API.</summary>
    public static OpenLibraryDoc? Read(JsonElement doc)
    {
        if (!doc.TryGetProperty("key", out var key) || key.GetString() is not { } keyValue)
        {
            return null;
        }

        var title = Chuoi(doc, "title");

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new OpenLibraryDoc(
            keyValue,
            title,
            Mang(doc, "author_name"),
            So(doc, "first_publish_year"),
            // Một tác phẩm có hàng chục ISBN của nhiều lần in; lấy cái đầu là đủ để tra lại.
            Mang(doc, "isbn").FirstOrDefault(),
            SoDai(doc, "cover_i"),
            Mang(doc, "publisher").FirstOrDefault(),
            Mang(doc, "subject").Take(8).ToList(),
            Mang(doc, "language").FirstOrDefault(),
            So(doc, "number_of_pages_median"));
    }

    /// <summary>Dựng biểu ghi MARC 21 từ một biểu ghi Open Library.</summary>
    public static MarcRecord ToMarc(OpenLibraryDoc doc, string? maCoQuanMinh = null)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var record = new MarcRecord();

        // Sách in, mức mô tả đầy đủ: dữ liệu Open Library đủ để công bố ngay.
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.Leader.EncodingLevel = ' ';
        record.Leader.DescriptiveCatalogingForm = 'i';

        var title = record.AddField("245", '1', '0');
        title.AddSubfield('a', doc.Title.Trim());

        if (doc.Authors.Count > 0)
        {
            title.AddSubfield('c', string.Join(", ", doc.Authors.Take(3)));
            record.AddField("100", '1').AddSubfield('a', doc.Authors[0]);

            foreach (var them in doc.Authors.Skip(1).Take(4))
            {
                record.AddField("700", '1').AddSubfield('a', them);
            }
        }

        if (!string.IsNullOrWhiteSpace(doc.Isbn))
        {
            record.AddField("020").AddSubfield('a', doc.Isbn.Trim());
        }

        if (doc.Publisher is not null || doc.FirstPublishYear is not null)
        {
            var xuatBan = record.AddField("264", ' ', '1');

            if (doc.Publisher is not null)
            {
                xuatBan.AddSubfield('b', doc.Publisher.Trim());
            }

            xuatBan.AddSubfield(
                'c',
                doc.FirstPublishYear?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                ?? DublinCore.KhongRoNam);
        }

        if (doc.PageCount is { } trang and > 0)
        {
            record.AddField("300").AddSubfield('a', $"{trang} tr.");
        }

        record.AddField("041").AddSubfield('a', LanguageCodes.ToMarc21(doc.Language));

        foreach (var subject in doc.Subjects)
        {
            // 650 với chỉ thị 2 = 7 và $2 nói rõ bảng đề mục — đúng cách MARC 21 ghi đề mục lấy từ
            // một bảng không phải LCSH.
            var field = record.AddField("650", ' ', '7');
            field.AddSubfield('a', subject.Trim());
            field.AddSubfield('2', "openlibrary");
        }

        // Bộ ba RDA cho sách in.
        Rda(record, "336", "text", "txt", "rdacontent");
        Rda(record, "337", "unmediated", "n", "rdamedia");
        Rda(record, "338", "volume", "nc", "rdacarrier");

        // 035$a giữ số kiểm soát của nơi phát, đúng quy ước (mã cơ quan)số kiểm soát.
        record.AddField("035").AddSubfield('a', $"(openlibrary.org){doc.Key.Trim('/')}");

        // 040: $a nơi biên mục gốc, $b ngôn ngữ biên mục, $d nơi hiệu đính.
        var nguon = record.AddField("040");
        nguon.AddSubfield('a', "openlibrary.org");
        nguon.AddSubfield('b', "vie");

        if (!string.IsNullOrWhiteSpace(maCoQuanMinh))
        {
            nguon.AddSubfield('d', maCoQuanMinh.Trim());
        }

        // 856$u trỏ về trang của tác phẩm bên Open Library, để tra ngược được.
        var lienKet = record.AddField("856", '4', '2');
        lienKet.AddSubfield('u', $"https://openlibrary.org{doc.Key}");
        lienKet.AddSubfield('y', "Xem ở Open Library");

        return record;
    }

    private static void Rda(MarcRecord record, string tag, string a, string b, string nguon)
    {
        var field = record.AddField(tag);
        field.AddSubfield('a', a);
        field.AddSubfield('b', b);
        field.AddSubfield('2', nguon);
    }

    private static string? Chuoi(JsonElement doc, string ten) =>
        doc.TryGetProperty(ten, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? So(JsonElement doc, string ten) =>
        doc.TryGetProperty(ten, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static long? SoDai(JsonElement doc, string ten) =>
        doc.TryGetProperty(ten, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : null;

    private static IReadOnlyList<string> Mang(JsonElement doc, string ten)
    {
        if (!doc.TryGetProperty(ten, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }
}
