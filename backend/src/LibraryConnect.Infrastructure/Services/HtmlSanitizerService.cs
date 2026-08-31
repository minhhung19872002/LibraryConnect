using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using LibraryConnect.Application.Common.Interfaces;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Bộ lọc HTML cho nội dung soạn bằng trình soạn thảo (VIII.1, VIII.2).
///
/// Danh sách thẻ cho phép được dựng theo đúng những gì trình soạn thảo của phần mềm sinh ra: chữ,
/// tiêu đề, danh sách, bảng, ảnh, liên kết, video nhúng. Mọi thứ ngoài danh sách bị bỏ — kể cả khi
/// một phiên bản trình soạn thảo sau này sinh ra thẻ lạ, thì cùng lắm là mất định dạng chứ không
/// bao giờ thành lỗ hổng.
/// </summary>
public class HtmlSanitizerService : Application.Common.Interfaces.IHtmlSanitizer
{
    /// <summary>Chỉ những nơi này được nhúng khung video; nơi khác thì thẻ iframe bị bỏ.</summary>
    private static readonly string[] AllowedFrameHosts =
    {
        "www.youtube.com", "youtube.com", "www.youtube-nocookie.com",
        "player.vimeo.com", "drive.google.com"
    };

    private static readonly Regex WhitespaceRuns = new(@"\s+", RegexOptions.Compiled);

    private readonly HtmlSanitizer _sanitizer = Build();

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(html);
    }

    public string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // Thẻ đóng khối phải thành khoảng trắng, nếu không thì "<p>một</p><p>hai</p>" dính lại
        // thành "mộthai" và phần tóm tắt tự sinh đọc không ra chữ.
        var spaced = Regex.Replace(html, "<[^>]+>", " ");
        var text = WebUtility.HtmlDecode(spaced);

        return WhitespaceRuns.Replace(text, " ").Trim();
    }

    private static HtmlSanitizer Build()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();

        foreach (var tag in new[]
                 {
                     "p", "br", "hr", "span", "div", "blockquote", "pre", "code",
                     "h1", "h2", "h3", "h4", "h5", "h6",
                     "strong", "b", "em", "i", "u", "s", "sub", "sup", "mark", "small",
                     "ul", "ol", "li", "dl", "dt", "dd",
                     "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
                     "a", "img", "figure", "figcaption", "iframe", "video", "source", "audio"
                 })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();

        foreach (var attribute in new[]
                 {
                     "href", "title", "target", "rel",
                     "src", "alt", "width", "height", "loading",
                     "colspan", "rowspan", "align", "class",
                     "controls", "poster", "type",
                     "allow", "allowfullscreen", "frameborder"
                 })
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }

        // Thuộc tính style mở ra cả một họ chiêu trò (đè lớp trong suốt lên nút bấm, tải ảnh từ
        // ngoài để dò người xem). Định dạng đã có sẵn qua thẻ và lớp CSS của giao diện.
        sanitizer.AllowedAttributes.Remove("style");
        sanitizer.AllowedCssProperties.Clear();

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");

        sanitizer.KeepChildNodes = true;

        sanitizer.PostProcessNode += (_, args) =>
        {
            if (args.Node is not AngleSharp.Html.Dom.IHtmlInlineFrameElement frame)
            {
                return;
            }

            if (!IsAllowedFrame(frame.Source))
            {
                frame.Remove();
            }
        };

        sanitizer.PostProcessNode += (_, args) =>
        {
            // Liên kết ra ngoài mở tab mới thì phải kèm rel để trang đích không điều khiển được tab
            // gốc — đây là mặc định an toàn, cán bộ soạn tin không phải nhớ.
            if (args.Node is AngleSharp.Html.Dom.IHtmlAnchorElement anchor
                && string.Equals(anchor.Target, "_blank", StringComparison.OrdinalIgnoreCase))
            {
                anchor.SetAttribute("rel", "noopener noreferrer");
            }
        };

        return sanitizer;
    }

    private static bool IsAllowedFrame(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)
            || !Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return AllowedFrameHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }
}
