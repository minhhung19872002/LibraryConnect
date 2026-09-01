using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Cms;
using LibraryConnect.Application.Features.Opac;
using LibraryConnect.Application.Features.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Nhóm endpoint công khai dùng cho OPAC và ứng dụng di động, không cần đăng nhập.
///
/// Tất cả đều chỉ trả nội dung đã xuất bản. Đây là mặt tiền của thư viện trên Internet, nên mọi
/// phương thức ở đây đều là đọc — không có endpoint nào ghi dữ liệu mà không cần đăng nhập.
/// </summary>
[Route("api/public")]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Tags("Công khai (OPAC / ứng dụng khách)")]
public class PublicController : ApiControllerBase
{
    /// <summary>Thông tin thư viện: tên, logo, địa chỉ, liên hệ và các tùy chọn hiển thị của OPAC.</summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<PublicSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PublicSettingsDto>>> GetSettings(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicSettingsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Nội dung trang chủ: sách mới, sách được mượn nhiều, tin tức, banner và liên kết.</summary>
    [HttpGet("home")]
    [ProducesResponseType(typeof(ApiResponse<OpacHomeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OpacHomeDto>>> Home(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOpacHomeQuery(), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // Tin tức
    // ---------------------------------------------------------------

    /// <summary>Danh sách tin tức đã đăng.</summary>
    [HttpGet("news")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacHomeNewsDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacHomeNewsDto>>>> News(
        [FromQuery] PublicNewsRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicNewsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chuyên mục tin kèm số bài đã đăng.</summary>
    [HttpGet("news/categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PublicNewsCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicNewsCategoryDto>>>> NewsCategories(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicNewsCategoriesQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Một bản tin đọc theo đường dẫn thân thiện.</summary>
    [HttpGet("news/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<PublicNewsDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PublicNewsDetailDto>>> NewsDetail(
        string slug, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicNewsDetailQuery(slug), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // Trang tĩnh và bố cục
    // ---------------------------------------------------------------

    /// <summary>Danh sách trang tĩnh đã đăng.</summary>
    [HttpGet("pages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsPageRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsPageRowDto>>>> Pages(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicPagesQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Nội dung một trang tĩnh.</summary>
    [HttpGet("pages/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CmsPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CmsPageDto>>> Page(string slug, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPublicPageQuery(slug), ct);
        return Ok(Success(result));
    }

    /// <summary>Cây menu điều hướng đang bật.</summary>
    [HttpGet("menus")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsMenuDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsMenuDto>>>> Menus(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsMenusQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Banner đang trong thời gian hiển thị.</summary>
    [HttpGet("banners")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsBannerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsBannerDto>>>> Banners(
        [FromQuery] string? position, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsBannersQuery(ActiveOnly: true, Position: position), ct);
        return Ok(Success(result));
    }

    /// <summary>Liên kết website: thư viện bạn và cơ sở dữ liệu trực tuyến.</summary>
    [HttpGet("links")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsExternalLinkDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsExternalLinkDto>>>> Links(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsLinksQuery(ActiveOnly: true), ct);
        return Ok(Success(result));
    }

    /// <summary>Album ảnh sự kiện đã đăng.</summary>
    [HttpGet("galleries")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsGalleryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsGalleryDto>>>> Galleries(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsGalleriesQuery(PublishedOnly: true), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // IX.5 — Tìm ở thư viện khác
    // ---------------------------------------------------------------

    /// <summary>Danh sách thư viện bạn mà bạn đọc tra sang được.</summary>
    [HttpGet("interlibrary/targets")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacRemoteTargetInfoDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpacRemoteTargetInfoDto>>>> RemoteTargets(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOpacRemoteTargetsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Tra cứu song song sang các thư viện bạn đã kết nối.</summary>
    /// <remarks>
    /// Chỉ tra ở những máy chủ cán bộ đã bật cờ hiện trên trang tra cứu, và mỗi nơi lấy tối đa 10
    /// biểu ghi — đây là endpoint mở ra Internet nên không để một lượt tra kéo dài vô hạn.
    /// </remarks>
    [HttpPost("interlibrary/search")]
    [ProducesResponseType(typeof(ApiResponse<OpacRemoteSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OpacRemoteSearchResultDto>>> RemoteSearch(
        [FromBody] OpacRemoteSearchQuery query, CancellationToken ct)
    {
        var result = await Mediator.Send(query, ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Ảnh bìa dựng sẵn cho một biểu ghi chưa có ảnh thật.
    ///
    /// Trả về SVG dựng từ chính dữ liệu thư mục: nhan đề, tác giả, năm, dạng tài liệu. Ảnh dựng lại
    /// được y hệt nên đặt bộ nhớ đệm dài hạn ở trình duyệt — trang kết quả tra cứu có hai chục ô bìa
    /// nên đây là chỗ đáng tiết kiệm nhất.
    /// </summary>
    [HttpGet("covers/{bibId:guid}.svg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cover(Guid bibId, CancellationToken ct)
    {
        var cover = await Mediator.Send(new GetBibCoverQuery(bibId), ct);

        if (Request.Headers.IfNoneMatch.Contains(cover.ETag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = cover.ETag;
        Response.Headers.CacheControl = "public, max-age=604800";

        return Content(cover.Svg, "image/svg+xml; charset=utf-8");
    }

    /// <summary>Ảnh dùng trong nội dung: logo, banner, ảnh tin, ảnh album.</summary>
    [HttpGet("media/{**objectName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Media(string objectName, CancellationToken ct)
    {
        var media = await Mediator.Send(new GetCmsMediaQuery(objectName), ct);

        // Ảnh nội dung đổi thì đổi cả tên tệp (mỗi lần tải lên sinh mã ngẫu nhiên mới), nên trình
        // duyệt giữ bao lâu cũng được — không có chuyện xem phải ảnh cũ.
        Response.Headers.CacheControl = "public, max-age=604800";

        return File(media.Content, media.ContentType);
    }
}
