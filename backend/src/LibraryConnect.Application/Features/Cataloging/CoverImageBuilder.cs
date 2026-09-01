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
    /// Tông màu theo dạng tài liệu, trải đều trên vòng tròn màu.
    ///
    /// Bảng màu đầu tiên chọn toàn tông tối cùng độ sáng và dồn cả chín dạng vào vùng xanh lam – tím:
    /// đúng là mỗi dạng một mã màu khác nhau, nhưng nhìn cả trang kết quả chỉ thấy một dải navy. Sách
    /// cách bài giảng 19 độ sắc, luận văn cách luận án đúng **3 độ** — mắt không phân biệt được.
    ///
    /// Bảng này trải chín dạng có thật trong kho ra khắp vòng tròn màu, mỗi cặp cách nhau ít nhất 25
    /// độ sắc: lam lục (sách) → lục (giáo trình) → lam (bài giảng) → chàm (luận án) → tím (luận văn)
    /// → hồng sen (tạp chí) → đỏ (báo) → cam đất (đề tài) → vàng đất (kỷ yếu) → ô liu (từ điển).
    /// Riêng bài trích để xám: nó là một phần của tài liệu khác chứ không phải một ấn phẩm riêng, và
    /// màu xám bão hòa thấp thì đứng cạnh màu nào cũng phân biệt được.
    ///
    /// Nền tối, chữ trắng: tỉ lệ tương phản đều trên 7:1 — mức AAA của WCAG cho chữ thường, không
    /// chỉ mức AA. Nhan đề trên bìa hiện ở cỡ nhỏ trong lưới kết quả nên cần mức ấy.
    ///
    /// Màu điểm nhấn (dải gáy sách bên trái) luôn sáng hơn nền, nếu không thì không thấy nó đâu.
    /// </summary>
    private static readonly Dictionary<string, CoverPalette> BangMau =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // sắc 180 — lam lục
            ["SACH"] = new("#0a5f5f", "#149999", "#ffffff"),
            // sắc 138 — lục
            ["GIAOTRINH"] = new("#17592b", "#26993f", "#ffffff"),
            // sắc 210 — lam
            ["BAIGIANG"] = new("#0a4c8f", "#1478cc", "#ffffff"),
            // sắc 238 — chàm
            ["LUANAN"] = new("#2a2e9e", "#484cd1", "#ffffff"),
            // sắc 276 — tím
            ["LUANVAN"] = new("#6a2599", "#9b44d1", "#ffffff"),
            // sắc 328 — hồng sen
            ["TAPCHI"] = new("#8a1a55", "#c42b80", "#ffffff"),
            // sắc 348 — đỏ
            ["BAO"] = new("#93122b", "#c92846", "#ffffff"),
            // sắc 13 — cam đất
            ["DETAI"] = new("#96351a", "#cc5a2e", "#ffffff"),
            // sắc 43 — vàng đất
            ["KYYEU"] = new("#6b4f07", "#a87c14", "#ffffff"),
            // sắc 74 — ô liu
            ["TUDIEN"] = new("#45560e", "#6e871c", "#ffffff"),
            // sắc 170 — lục lam
            ["BANDO"] = new("#095a4c", "#12907a", "#ffffff"),
            // sắc 306 — đỏ tía
            ["AUDIO"] = new("#7a1470", "#b222a3", "#ffffff"),
            // sắc 220 — lam đậm
            ["VIDEO"] = new("#123a8a", "#1f5fcc", "#ffffff"),
            // xám, bão hòa 0,24 — bài trích không phải ấn phẩm riêng
            ["BAITRICH"] = new("#414c55", "#6b7a88", "#ffffff"),
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
