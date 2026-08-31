using LibraryConnect.Application.Features.Opac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    public SeoController(MediatR.ISender mediator) => _mediator = mediator;

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
