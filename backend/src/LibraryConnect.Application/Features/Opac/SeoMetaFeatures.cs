using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Public;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.1 — Thẻ meta phía máy chủ.
//
// Trang tra cứu là SPA: tệp index.html mà máy chủ trả về cho mọi địa chỉ chỉ có một tiêu đề chung
// "Tra cứu thư viện". Máy thu thập của Facebook, Zalo, Google không chạy JavaScript (hoặc chạy
// muộn), nên bạn đọc dán liên kết một cuốn sách lên Zalo là ô xem trước hiện "Tra cứu thư viện"
// chứ không hiện tên sách. Với ba loại địa chỉ có nội dung riêng — tài liệu, bản tin, trang tĩnh —
// máy chủ chèn sẵn nhan đề, mô tả và thẻ Open Graph vào đúng index.html ấy rồi mới trả về.
// ---------------------------------------------------------------------------------------------

/// <summary>Bộ thẻ meta của một địa chỉ công khai.</summary>
public record SeoMeta(string Title, string Description, string Url, string? ImageUrl, string Type);

/// <summary>Chèn thẻ meta vào <c>index.html</c> của trang tra cứu.</summary>
public static class SeoHtml
{
    private static readonly Regex TitleTag = new(@"<title>.*?</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex DescriptionTag = new(@"<meta\s+name=[""']description[""'][^>]*>", RegexOptions.IgnoreCase);
    private static readonly Regex HeadEnd = new(@"</head>", RegexOptions.IgnoreCase);

    /// <summary>Số ký tự tối đa của mô tả — dài hơn thì Facebook và Google tự cắt, mà cắt giữa chừng thì xấu.</summary>
    public const int DescriptionLength = 200;

    /// <summary>
    /// Thay tiêu đề, thay mô tả sẵn có (không để hai thẻ description trong một trang) và thêm các
    /// thẻ Open Graph ngay trước <c>&lt;/head&gt;</c>. Không có <c>&lt;/head&gt;</c> thì trả nguyên —
    /// tệp lạ thì không chèn bừa.
    /// </summary>
    public static string Inject(string indexHtml, SeoMeta meta, string siteName)
    {
        ArgumentNullException.ThrowIfNull(indexHtml);
        ArgumentNullException.ThrowIfNull(meta);

        if (!HeadEnd.IsMatch(indexHtml))
        {
            return indexHtml;
        }

        var fullTitle = string.IsNullOrWhiteSpace(siteName) ? meta.Title : $"{meta.Title} – {siteName}";
        var description = Truncate(meta.Description, DescriptionLength);

        var html = TitleTag.IsMatch(indexHtml)
            ? TitleTag.Replace(indexHtml, $"<title>{Encode(fullTitle)}</title>", 1)
            : HeadEnd.Replace(indexHtml, $"<title>{Encode(fullTitle)}</title></head>", 1);

        var descriptionTag = $"<meta name=\"description\" content=\"{Encode(description)}\" />";

        html = DescriptionTag.IsMatch(html)
            ? DescriptionTag.Replace(html, descriptionTag, 1)
            : HeadEnd.Replace(html, descriptionTag + "</head>", 1);

        var tags = new StringBuilder()
            .Append("<meta property=\"og:type\" content=\"").Append(Encode(meta.Type)).Append("\" />")
            .Append("<meta property=\"og:title\" content=\"").Append(Encode(meta.Title)).Append("\" />")
            .Append("<meta property=\"og:description\" content=\"").Append(Encode(description)).Append("\" />")
            .Append("<meta property=\"og:url\" content=\"").Append(Encode(meta.Url)).Append("\" />")
            .Append("<meta property=\"og:site_name\" content=\"").Append(Encode(siteName)).Append("\" />");

        if (!string.IsNullOrWhiteSpace(meta.ImageUrl))
        {
            tags.Append("<meta property=\"og:image\" content=\"").Append(Encode(meta.ImageUrl)).Append("\" />");
        }

        tags.Append("<meta name=\"twitter:card\" content=\"summary\" />");

        return HeadEnd.Replace(html, tags + "</head>", 1);
    }

    /// <summary>
    /// Trang tối thiểu dùng khi không lấy được index.html thật: máy thu thập vẫn có thẻ meta, còn
    /// người thật thì có liên kết mở trang. Chỉ máy thu thập mới tới đây (Nginx rẽ theo User-Agent).
    /// </summary>
    public static string FallbackShell(string siteName) =>
        "<!doctype html><html lang=\"vi\"><head><meta charset=\"UTF-8\" />"
        + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />"
        + $"<title>{Encode(siteName)}</title></head>"
        + "<body><div id=\"root\"><p>Đang mở trang tra cứu…</p><p><a href=\"/\">Về trang chủ</a></p></div></body></html>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text ?? string.Empty);

    private static string Truncate(string text, int length)
    {
        var compact = Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();

        return compact.Length <= length ? compact : compact[..(length - 1)].TrimEnd() + "…";
    }
}

/// <summary>
/// Thẻ meta cho một địa chỉ: <paramref name="Kind"/> là <c>bib</c>, <c>news</c> hay <c>page</c>;
/// <paramref name="Key"/> là mã tài liệu hoặc slug; <paramref name="BaseUrl"/> là địa chỉ gốc mà
/// người dùng nhìn thấy (để thẻ og:url và og:image là địa chỉ tuyệt đối).
/// </summary>
public record GetSeoMetaQuery(string Kind, string Key, string BaseUrl) : IRequest<(SeoMeta Meta, string SiteName)>;

public class GetSeoMetaQueryHandler : IRequestHandler<GetSeoMetaQuery, (SeoMeta Meta, string SiteName)>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IHtmlSanitizer _html;
    private readonly IDateTimeProvider _clock;

    public GetSeoMetaQueryHandler(
        IApplicationDbContext db, ISystemParameterService parameters, IHtmlSanitizer html, IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _html = html;
        _clock = clock;
    }

    public async Task<(SeoMeta Meta, string SiteName)> Handle(GetSeoMetaQuery query, CancellationToken ct)
    {
        var siteName = await _parameters.GetAsync(ParameterKeysBridge.LibraryName, ct);

        if (string.IsNullOrWhiteSpace(siteName))
        {
            siteName = "Thư viện";
        }

        var baseUrl = query.BaseUrl.TrimEnd('/');

        var meta = query.Kind switch
        {
            "bib" => await BibAsync(query.Key, baseUrl, ct),
            "news" => await NewsAsync(query.Key, baseUrl, ct),
            "page" => await PageAsync(query.Key, baseUrl, ct),
            _ => throw new NotFoundException("Không tìm thấy trang."),
        };

        return (meta, siteName);
    }

    private async Task<SeoMeta> BibAsync(string key, string baseUrl, CancellationToken ct)
    {
        if (!Guid.TryParse(key, out var id))
        {
            throw new NotFoundException("Không tìm thấy tài liệu.");
        }

        var bib = await OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .Where(row => row.Id == id)
            .Select(row => new
            {
                row.Title,
                row.Subtitle,
                row.AuthorMain,
                row.PublishYear,
                row.Abstract,
                row.CoverImageUrl,
                Publisher = row.Publisher != null ? row.Publisher.Name : null,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        var title = string.IsNullOrWhiteSpace(bib.Subtitle) ? bib.Title : $"{bib.Title}: {bib.Subtitle}";

        // Không có tóm tắt thì mô tả là dòng thư mục ngắn: tác giả, nhà xuất bản, năm — vẫn hơn câu chung chung.
        var description = !string.IsNullOrWhiteSpace(bib.Abstract)
            ? bib.Abstract
            : string.Join(" · ", new[] { bib.AuthorMain, bib.Publisher, bib.PublishYear?.ToString() }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        if (string.IsNullOrWhiteSpace(description))
        {
            description = title;
        }

        return new SeoMeta(title, description, $"{baseUrl}/tai-lieu/{id}", Absolute(baseUrl, bib.CoverImageUrl), "book");
    }

    private async Task<SeoMeta> NewsAsync(string slug, string baseUrl, CancellationToken ct)
    {
        var now = _clock.Now;

        var news = await _db.CmsNews.AsNoTracking()
            .Where(row => row.Slug == slug && row.IsPublished && (row.PublishedAt == null || row.PublishedAt <= now))
            .Select(row => new { row.Title, row.Summary, row.Content, row.ThumbnailUrl })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Không tìm thấy bản tin.");

        var description = !string.IsNullOrWhiteSpace(news.Summary) ? news.Summary : _html.ToPlainText(news.Content);

        return new SeoMeta(news.Title, description, $"{baseUrl}/tin-tuc/{slug}", Absolute(baseUrl, news.ThumbnailUrl), "article");
    }

    private async Task<SeoMeta> PageAsync(string slug, string baseUrl, CancellationToken ct)
    {
        var page = await _db.CmsPages.AsNoTracking()
            .Where(row => row.Slug == slug && row.IsPublished)
            .Select(row => new { row.Title, row.MetaDescription, row.Content })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Không tìm thấy trang.");

        var description = !string.IsNullOrWhiteSpace(page.MetaDescription)
            ? page.MetaDescription
            : _html.ToPlainText(page.Content);

        return new SeoMeta(page.Title, description, $"{baseUrl}/trang/{slug}", null, "website");
    }

    /// <summary>Ảnh bìa lưu dạng đường dẫn tương đối (/api/...); máy thu thập cần địa chỉ tuyệt đối.</summary>
    private static string? Absolute(string baseUrl, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"{baseUrl}/{url.TrimStart('/')}";
    }
}
