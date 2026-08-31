using System.Text;
using System.Xml.Linq;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.1 — SEO: sơ đồ trang và tệp hướng dẫn cho máy thu thập.
//
// Sơ đồ trang liệt kê trang tĩnh, bản tin và tài liệu đã xuất bản. Đây là cách một thư viện được
// tìm thấy từ ngoài: sinh viên gõ tên sách trên công cụ tìm kiếm và ra thẳng trang tra cứu.
// ---------------------------------------------------------------------------------------------

public record GetSitemapQuery(string BaseUrl) : IRequest<string>;

public class GetSitemapQueryHandler : IRequestHandler<GetSitemapQuery, string>
{
    /// <summary>
    /// Giới hạn số tài liệu đưa vào sơ đồ.
    ///
    /// Chuẩn sơ đồ trang cho tối đa 50.000 địa chỉ mỗi tệp. Kho lớn hơn thế thì phần còn lại vẫn
    /// được tìm thấy qua các trang duyệt theo chủ đề và theo phân loại, vốn cũng nằm trong sơ đồ.
    /// </summary>
    private const int MaxBibs = 40_000;

    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetSitemapQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<string> Handle(GetSitemapQuery query, CancellationToken ct)
    {
        var baseUrl = query.BaseUrl.TrimEnd('/');
        var urls = new List<XElement>
        {
            Url($"{baseUrl}/", _clock.Now, "daily", "1.0"),
            Url($"{baseUrl}/tra-cuu", _clock.Now, "daily", "0.9"),
            Url($"{baseUrl}/duyet/chu-de", _clock.Now, "weekly", "0.7"),
            Url($"{baseUrl}/duyet/tac-gia", _clock.Now, "weekly", "0.7"),
            Url($"{baseUrl}/duyet/phan-loai", _clock.Now, "weekly", "0.7"),
            Url($"{baseUrl}/tin-tuc", _clock.Now, "daily", "0.8")
        };

        var pages = await _db.CmsPages.AsNoTracking()
            .Where(page => page.IsPublished)
            .Select(page => new { page.Slug, page.UpdatedAt, page.CreatedAt })
            .ToListAsync(ct);

        urls.AddRange(pages.Select(page =>
            Url($"{baseUrl}/trang/{page.Slug}", page.UpdatedAt ?? page.CreatedAt, "monthly", "0.6")));

        var news = await GetOpacHomeQueryHandler.PublishedNews(_db, _clock.Now)
            .OrderByDescending(item => item.PublishedAt)
            .Take(2_000)
            .Select(item => new { item.Slug, item.UpdatedAt, item.PublishedAt })
            .ToListAsync(ct);

        urls.AddRange(news.Select(item =>
            Url($"{baseUrl}/tin-tuc/{item.Slug}",
                item.UpdatedAt ?? item.PublishedAt ?? _clock.Now, "monthly", "0.6")));

        var bibs = await OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .OrderByDescending(bib => bib.UpdatedAt ?? bib.CreatedAt)
            .Take(MaxBibs)
            .Select(bib => new { bib.Id, bib.UpdatedAt, bib.CreatedAt })
            .ToListAsync(ct);

        urls.AddRange(bibs.Select(bib =>
            Url($"{baseUrl}/tai-lieu/{bib.Id}", bib.UpdatedAt ?? bib.CreatedAt, "monthly", "0.5")));

        var document = new XDocument(new XElement(Ns + "urlset", urls));

        return document.Declaration is null
            ? "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + document
            : document.ToString();
    }

    private static XElement Url(string location, DateTimeOffset lastModified, string frequency, string priority) =>
        new(Ns + "url",
            new XElement(Ns + "loc", location),
            new XElement(Ns + "lastmod", lastModified.ToString("yyyy-MM-dd")),
            new XElement(Ns + "changefreq", frequency),
            new XElement(Ns + "priority", priority));
}

/// <summary>
/// Tệp hướng dẫn máy thu thập.
///
/// Mở phần tra cứu công khai, chặn khu quản trị và các đường dẫn cá nhân của bạn đọc — không có lý
/// do gì để trang "sách tôi đang mượn" nằm trên công cụ tìm kiếm.
/// </summary>
public record GetRobotsTxtQuery(string BaseUrl) : IRequest<string>;

public class GetRobotsTxtQueryHandler : IRequestHandler<GetRobotsTxtQuery, string>
{
    public Task<string> Handle(GetRobotsTxtQuery query, CancellationToken ct)
    {
        var builder = new StringBuilder();

        builder.AppendLine("User-agent: *");
        builder.AppendLine("Allow: /");
        builder.AppendLine("Disallow: /admin");
        builder.AppendLine("Disallow: /api/");
        builder.AppendLine("Disallow: /tai-khoan");
        builder.AppendLine("Disallow: /gio-tai-lieu");
        builder.AppendLine();
        builder.AppendLine($"Sitemap: {query.BaseUrl.TrimEnd('/')}/sitemap.xml");

        return Task.FromResult(builder.ToString());
    }
}
