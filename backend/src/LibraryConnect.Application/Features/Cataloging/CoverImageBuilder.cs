using System.Globalization;
using System.Text;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Những gì cần biết về một biểu ghi để dựng ảnh bìa thay thế.</summary>
public record CoverInput(
    string Title,
    string? Author,
    int? PublishYear,
    string? DocumentTypeCode,
    string? DocumentTypeName);

/// <summary>Tông màu của một dạng tài liệu.</summary>
public record CoverPalette(string Background, string Accent, string Foreground);

/// <summary>
/// Dựng ảnh bìa thay thế cho biểu ghi chưa có ảnh thật.
///
/// Đây không phải thứ dùng tạm. Đo trên kho thật: **444 trên 7.675 biểu ghi có ISBN (5,8%)**, mà
/// không có ISBN thì không nguồn nào tra ra ảnh bìa. Luận văn, đề tài nghiên cứu và bài giảng điện
/// tử — ba nhóm chiếm hơn hai phần ba kho — không bao giờ có ảnh bìa trên mạng. Với hơn 94% biểu
/// ghi, ảnh dựng ở đây **là** ảnh bìa chính thức, nên nó phải đọc được và phân biệt được.
///
/// Dựng bằng SVG chứ không phải ảnh điểm ảnh: nét ở mọi kích thước, nặng chưa tới 2 KB, và dựng lại
/// được y hệt nên đặt được bộ nhớ đệm dài hạn ở trình duyệt.
///
/// Màu theo **dạng tài liệu** chứ không theo nhan đề: bạn đọc lướt trang kết quả là nhận ra ngay
/// đâu là luận văn, đâu là giáo trình, mà không phải đọc nhãn.
/// </summary>
public static class CoverImageBuilder
{
    public const int Width = 400;
    public const int Height = 600;

    /// <summary>Số ký tự tối đa một dòng nhan đề, ở cỡ chữ mặc định.</summary>
    private const int KyTuMoiDong = 18;

    /// <summary>Số dòng tối đa dành cho nhan đề.</summary>
    private const int SoDongToiDa = 6;

    /// <summary>
    /// Tông màu theo dạng tài liệu.
    ///
    /// Nền tối, chữ trắng: tỉ lệ tương phản đều trên 7:1, vượt mức AA của WCAG cho cả cỡ chữ nhỏ.
    /// </summary>
    private static readonly Dictionary<string, CoverPalette> BangMau =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SACH"] = new("#1f4e5f", "#2e7d8f", "#ffffff"),
            ["GIAOTRINH"] = new("#1f5f3a", "#2f8f57", "#ffffff"),
            ["LUANVAN"] = new("#4a3b6b", "#6f5aa0", "#ffffff"),
            ["LUANAN"] = new("#3b2f5e", "#5d4a91", "#ffffff"),
            ["DETAI"] = new("#6b3a2e", "#9c5744", "#ffffff"),
            ["KYYEU"] = new("#5f4a1f", "#8f722e", "#ffffff"),
            ["BAIGIANG"] = new("#1f3f6b", "#2f5f9c", "#ffffff"),
            ["BAITRICH"] = new("#4a4a4a", "#6f6f6f", "#ffffff"),
            ["TAPCHI"] = new("#6b2f4a", "#9c456f", "#ffffff"),
            ["BAO"] = new("#2f2f4a", "#47476f", "#ffffff"),
            ["TUDIEN"] = new("#3f5f1f", "#5f8f2f", "#ffffff"),
            ["BANDO"] = new("#1f5f5f", "#2f8f8f", "#ffffff"),
            ["AUDIO"] = new("#5f1f4a", "#8f2f70", "#ffffff"),
            ["VIDEO"] = new("#1f2f5f", "#2f478f", "#ffffff"),
        };

    /// <summary>Dùng cho dạng tài liệu chưa có trong bảng — vẫn phải đọc được.</summary>
    private static readonly CoverPalette MacDinh = new("#3a3a44", "#5a5a68", "#ffffff");

    public static CoverPalette PaletteFor(string? documentTypeCode) =>
        documentTypeCode is not null && BangMau.TryGetValue(documentTypeCode, out var palette)
            ? palette
            : MacDinh;

    /// <summary>
    /// Ngắt nhan đề thành từng dòng vừa bề rộng bìa.
    ///
    /// Cắt ở chỗ giáp từ chứ không cắt theo số ký tự: cắt giữa từ thì nhan đề tiếng Việt đọc thành
    /// một chuỗi vô nghĩa. Từ nào dài hơn cả một dòng — tên hóa chất, mã hiệu — thì đành cắt, nhưng
    /// đó là trường hợp hiếm chứ không phải cách làm mặc định.
    /// </summary>
    public static IReadOnlyList<string> WrapTitle(string title, int kyTuMoiDong, int soDongToiDa)
    {
        ArgumentNullException.ThrowIfNull(title);

        var gon = string.Join(' ', title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (gon.Length == 0)
        {
            return Array.Empty<string>();
        }

        var dong = new List<string>();
        var hienTai = new StringBuilder();

        foreach (var tu in TachTu(gon, kyTuMoiDong))
        {
            if (hienTai.Length == 0)
            {
                hienTai.Append(tu);
                continue;
            }

            if (hienTai.Length + 1 + tu.Length <= kyTuMoiDong)
            {
                hienTai.Append(' ').Append(tu);
                continue;
            }

            dong.Add(hienTai.ToString());
            hienTai.Clear().Append(tu);

            if (dong.Count == soDongToiDa)
            {
                break;
            }
        }

        if (dong.Count < soDongToiDa && hienTai.Length > 0)
        {
            dong.Add(hienTai.ToString());
        }

        // Còn chữ chưa hiện hết thì nói thẳng bằng dấu ba chấm, không im lặng cắt.
        if (string.Join(' ', dong).Length < gon.Length && dong.Count > 0)
        {
            var cuoi = dong[^1];

            dong[^1] = cuoi.Length >= kyTuMoiDong
                ? cuoi[..(kyTuMoiDong - 1)].TrimEnd() + "…"
                : cuoi + "…";
        }

        return dong;
    }

    /// <summary>Tách chuỗi thành từ, chẻ nhỏ từ nào dài hơn cả một dòng.</summary>
    private static IEnumerable<string> TachTu(string value, int kyTuMoiDong)
    {
        foreach (var tu in value.Split(' '))
        {
            if (tu.Length <= kyTuMoiDong)
            {
                yield return tu;
                continue;
            }

            for (var index = 0; index < tu.Length; index += kyTuMoiDong)
            {
                yield return tu.Substring(index, Math.Min(kyTuMoiDong, tu.Length - index));
            }
        }
    }

    /// <summary>Dựng ảnh bìa dạng SVG.</summary>
    public static string BuildSvg(CoverInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var palette = PaletteFor(input.DocumentTypeCode);
        var dong = WrapTitle(input.Title, KyTuMoiDong, SoDongToiDa);

        // Nhan đề ngắn thì cho chữ to lên; dài thì thu nhỏ để vẫn đủ chỗ. Đây là chỗ quyết định bìa
        // đọc được hay không ở kích thước nhỏ trên trang kết quả tra cứu.
        var coChu = dong.Count switch
        {
            <= 2 => 34,
            3 => 30,
            4 => 27,
            _ => 24,
        };

        var caoDong = (int)(coChu * 1.28);
        var giua = 250 - (dong.Count - 1) * caoDong / 2;

        var builder = new StringBuilder(2048);

        builder.Append(CultureInfo.InvariantCulture,
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {Width} {Height}" width="{Width}" height="{Height}" role="img">""");

        builder.Append(CultureInfo.InvariantCulture,
            $"""<title>{Thoat(input.Title)}</title>""");

        // Nền và dải gáy sách bên trái — dấu hiệu thị giác để nhận ra ngay đây là một cuốn sách.
        builder.Append(CultureInfo.InvariantCulture,
            $"""<rect width="{Width}" height="{Height}" fill="{palette.Background}"/>""");
        builder.Append(CultureInfo.InvariantCulture,
            $"""<rect width="18" height="{Height}" fill="{palette.Accent}"/>""");
        builder.Append(CultureInfo.InvariantCulture,
            $"""<rect x="46" y="86" width="{Width - 92}" height="2" fill="{palette.Accent}"/>""");

        builder.Append(
            """<g font-family="Be Vietnam Pro, Inter, Segoe UI, Arial, sans-serif" """
            + $"""fill="{palette.Foreground}" text-anchor="middle">""");

        for (var index = 0; index < dong.Count; index++)
        {
            var y = giua + index * caoDong;

            builder.Append(CultureInfo.InvariantCulture,
                $"""<text x="209" y="{y}" font-size="{coChu}" font-weight="600">{Thoat(dong[index])}</text>""");
        }

        if (!string.IsNullOrWhiteSpace(input.Author))
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"""<text x="209" y="{Height - 116}" font-size="20" opacity="0.88">{Thoat(CatGon(input.Author, 26))}</text>""");
        }

        if (input.PublishYear is { } nam)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"""<text x="209" y="{Height - 86}" font-size="18" opacity="0.72">{nam}</text>""");
        }

        builder.Append(CultureInfo.InvariantCulture,
            $"""<rect x="46" y="{Height - 62}" width="{Width - 92}" height="1" fill="{palette.Foreground}" opacity="0.35"/>""");

        builder.Append(CultureInfo.InvariantCulture,
            $"""<text x="209" y="{Height - 34}" font-size="17" letter-spacing="1.5" opacity="0.85">{Thoat(CatGon(input.DocumentTypeName ?? "Tài liệu", 24))}</text>""");

        builder.Append("</g></svg>");

        return builder.ToString();
    }

    private static string CatGon(string value, int gioiHan)
    {
        var gon = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return gon.Length <= gioiHan ? gon : gon[..(gioiHan - 1)].TrimEnd() + "…";
    }

    /// <summary>Thoát ký tự cho XML. Dấu tiếng Việt giữ nguyên vì tệp là UTF-8.</summary>
    private static string Thoat(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);
}
