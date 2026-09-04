using LibraryConnect.Application.Features.Opac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// IX.1 — Sơ đồ trang và tệp hướng dẫn máy thu thập.
///
/// Nằm ở gốc địa chỉ chứ không dưới /api, vì công cụ tìm kiếm chỉ đọc /sitemap.xml và /robots.txt.
/// Máy chủ Nginx chuyển hai đường dẫn này về đây.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Tags("Tra cứu (OPAC / ứng dụng khách)")]
public class SeoController : ControllerBase
{
    private readonly MediatR.ISender _mediator;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClients;

    public SeoController(
        MediatR.ISender mediator,
        IMemoryCache cache,
        IConfiguration configuration,
        IHttpClientFactory httpClients)
    {
        _mediator = mediator;
        _cache = cache;
        _configuration = configuration;
        _httpClients = httpClients;
    }

    /// <summary>Sơ đồ trang: trang tĩnh, bản tin và tài liệu đã xuất bản.</summary>
    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var xml = await _mediator.Send(new GetSitemapQuery(BaseUrl()), ct);
        return Content(xml, "application/xml; charset=utf-8");
    }

    /// <summary>Tệp hướng dẫn cho máy thu thập của công cụ tìm kiếm.</summary>
    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Robots(CancellationToken ct)
    {
        var text = await _mediator.Send(new GetRobotsTxtQuery(BaseUrl()), ct);
        return Content(text, "text/plain; charset=utf-8");
    }

    // ---------------------------------------------------------------
    // IX.1 — Thẻ meta phía máy chủ cho ba loại địa chỉ có nội dung riêng.
    //
    // Nginx rẽ các địa chỉ này về đây khi User-Agent là máy thu thập (Facebook, Zalo, Google...);
    // người thật vẫn nhận index.html thẳng từ container opac. Máy chủ lấy đúng index.html ấy qua
    // HTTP nội bộ (có bộ đệm), chèn nhan đề, mô tả và Open Graph rồi trả về — nên máy thu thập nào
    // chạy được JavaScript vẫn thấy đúng ứng dụng, chỉ khác là thẻ meta đã đúng từ đầu.
    // ---------------------------------------------------------------

    /// <summary>Trang chi tiết tài liệu với thẻ meta chèn sẵn.</summary>
    [HttpGet("/tai-lieu/{id:guid}")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> BibPage(Guid id, CancellationToken ct) => RenderAsync("bib", id.ToString(), ct);

    /// <summary>Trang bản tin với thẻ meta chèn sẵn.</summary>
    [HttpGet("/tin-tuc/{slug}")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> NewsPage(string slug, CancellationToken ct) => RenderAsync("news", slug, ct);

    /// <summary>Trang tĩnh với thẻ meta chèn sẵn.</summary>
    [HttpGet("/trang/{slug}")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> StaticPage(string slug, CancellationToken ct) => RenderAsync("page", slug, ct);

    private async Task<IActionResult> RenderAsync(string kind, string key, CancellationToken ct)
    {
        var (meta, siteName) = await _mediator.Send(new GetSeoMetaQuery(kind, key, BaseUrl()), ct);
        var shell = await OpacShellAsync(siteName, ct);

        // Thẻ meta đổi theo nội dung, nên không cho bộ đệm trung gian giữ lâu; index.html thật đã
        // có bộ đệm riêng ở dưới.
        Response.Headers.CacheControl = "public, max-age=300";

        return Content(SeoHtml.Inject(shell, meta, siteName), "text/html; charset=utf-8");
    }

    private const string ShellCacheKey = "seo:opac-index";

    /// <summary>
    /// index.html của trang tra cứu: đọc từ tệp nếu cấu hình <c>Seo:OpacIndexPath</c>, không thì
    /// lấy qua HTTP nội bộ từ <c>Seo:OpacIndexUrl</c> (mặc định là container opac). Giữ trong bộ
    /// đệm năm phút; không lấy được thì dùng trang tối thiểu — máy thu thập vẫn có thẻ meta.
    /// </summary>
    private async Task<string> OpacShellAsync(string siteName, CancellationToken ct)
    {
        if (_cache.TryGetValue(ShellCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var shell = await LoadShellAsync(ct);

        if (shell is not null)
        {
            _cache.Set(ShellCacheKey, shell, TimeSpan.FromMinutes(5));
            return shell;
        }

        return SeoHtml.FallbackShell(siteName);
    }

    private async Task<string?> LoadShellAsync(CancellationToken ct)
    {
        var path = _configuration["Seo:OpacIndexPath"];

        if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
        {
            return await System.IO.File.ReadAllTextAsync(path, ct);
        }

        var url = _configuration["Seo:OpacIndexUrl"] ?? "http://opac:80/";

        try
        {
            var client = _httpClients.CreateClient("opac-shell");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));

            using var response = await client.GetAsync(url, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(timeout.Token);

            // Chỉ nhận thứ trông như trang SPA; một trang lỗi của Nginx cũng là HTML nhưng không có gốc ứng dụng.
            return html.Contains("id=\"root\"", StringComparison.OrdinalIgnoreCase) ? html : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>
    /// Địa chỉ gốc mà người dùng nhìn thấy.
    ///
    /// Sau máy chủ Nginx thì địa chỉ của chính tiến trình này là http://api:8080, đưa vào sơ đồ
    /// trang là công cụ tìm kiếm nhận một địa chỉ không ai truy cập được. Nên đọc tiêu đề chuyển
    /// tiếp trước.
    /// </summary>
    private string BaseUrl()
    {
        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
        var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;

        return $"{scheme}://{host}";
    }
}
