using System.Globalization;
using System.Text;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ảnh minh họa cho bộ dữ liệu trình diễn (VIII.1 banner, VIII.2 thư viện ảnh).
///
/// Không có ảnh thật nào đi kèm mã nguồn, mà kho đối tượng lúc gieo dữ liệu thì còn trống — nên
/// ảnh mẫu là SVG nhúng thẳng vào địa chỉ (data URI): tự vẽ nền và chữ theo bảng màu của giao diện,
/// không cần tệp nào, và cán bộ thay bằng ảnh thật ở màn hình Banner / Thư viện ảnh.
///
/// Hai chỗ dễ hỏng, cả hai đều có phép thử canh:
///   1. Cột <c>image_url</c> giới hạn 1.000 ký tự. Địa chỉ dài hơn là lượt gieo dữ liệu đổ giữa
///      chừng, mà nó chạy lúc khởi động container nên hệ thống không lên nổi.
///   2. Data URI nằm trong thuộc tính <c>src</c>: dấu <c>#</c> cắt phần còn lại thành mảnh neo,
///      dấu <c>%</c> lạc chỗ làm hỏng cả chuỗi, dấu <c>&lt;</c> chưa mã hóa thì trình duyệt hiểu
///      nhầm là thẻ. Vì vậy mã hóa đủ và chỉ đủ những ký tự ấy.
/// </summary>
public static class DemoImages
{
    /// <summary>Giới hạn của cột <c>image_url</c> trong <c>cms_banners</c> và <c>cms_gallery_images</c>.</summary>
    public const int MaxUrlLength = 1000;

    /// <summary>
    /// Một ảnh SVG dạng data URI: nền màu <paramref name="fill"/> (mã màu thường, ví dụ
    /// <c>#35523f</c>), nhan đề chữ có chân và một dòng phụ nhỏ hơn.
    /// </summary>
    public static string Svg(string title, string subtitle, string fill, int width, int height)
    {
        var titleY = (height * 0.46).ToString("0", CultureInfo.InvariantCulture);
        var subtitleY = (height * 0.46 + 54).ToString("0", CultureInfo.InvariantCulture);
        var titleSize = width >= 1000 ? 58 : 40;
        var subtitleSize = width >= 1000 ? 26 : 20;

        var svg =
            $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}'>"
            + $"<rect width='{width}' height='{height}' fill='{fill}'/>"
            + $"<text x='56' y='{titleY}' font-family='Georgia,serif' font-size='{titleSize}' fill='#fffdf8'>{Xml(title)}</text>"
            + $"<text x='56' y='{subtitleY}' font-family='sans-serif' font-size='{subtitleSize}' fill='#f2ecdd'>{Xml(subtitle)}</text>"
            + "</svg>";

        return "data:image/svg+xml;charset=utf-8," + Escape(svg);
    }

    /// <summary>Chữ đi vào nội dung XML: chỉ ba ký tự này mới phá cấu trúc thẻ.</summary>
    private static string Xml(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");

    /// <summary>
    /// Mã hóa phần sau dấu phẩy của data URI. Không dùng <see cref="Uri.EscapeDataString"/> vì nó
    /// mã hóa cả dấu nháy đơn và dấu chấm phẩy — đúng chuẩn nhưng dài gấp rưỡi, mà cột chỉ chứa
    /// được 1.000 ký tự. Ở đây chỉ mã hóa những ký tự thật sự phá địa chỉ.
    /// </summary>
    private static string Escape(string svg)
    {
        var builder = new StringBuilder(svg.Length + 64);

        foreach (var character in svg)
        {
            switch (character)
            {
                case '%': builder.Append("%25"); break;
                case '#': builder.Append("%23"); break;
                case '<': builder.Append("%3C"); break;
                case '>': builder.Append("%3E"); break;
                case '"': builder.Append("%22"); break;
                case '&': builder.Append("%26"); break;
                case ' ': builder.Append("%20"); break;
                default: builder.Append(character); break;
            }
        }

        return builder.ToString();
    }
}
