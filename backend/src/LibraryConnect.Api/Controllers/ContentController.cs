using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Cms;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ VIII — Quản trị nội dung: thông tin trang thư viện, trang tĩnh, tin tức, menu, banner,
/// liên kết website, thư viện ảnh và kiểm duyệt nhận xét bạn đọc.
/// </summary>
[Route("api/content")]
[Tags("Quản trị nội dung")]
public class ContentController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // VIII.1 — Thông tin trang thư viện
    // ---------------------------------------------------------------

    /// <summary>Toàn bộ ô cấu hình hiển thị của trang tra cứu, gom theo nhóm.</summary>
    [HttpGet("settings")]
    [RequirePermission(PermissionCodes.CmsSettingManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsSettingGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsSettingGroupDto>>>> Settings(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsSettingsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu các ô cấu hình đã sửa.</summary>
    [HttpPut("settings")]
    [RequirePermission(PermissionCodes.CmsSettingManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateSettings(
        [FromBody] UpdateCmsSettingsCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đã lưu cấu hình trang thư viện."));
    }

    /// <summary>
    /// Tải ảnh hoặc tệp đính kèm dùng cho nội dung: logo, banner, ảnh tin, ảnh album, tệp PDF/Word/Excel
    /// chèn vào bài viết.
    /// </summary>
    /// <remarks>
    /// Ai soạn được nội dung nào thì chèn được tệp vào nội dung ấy: người viết tin không có quyền
    /// cấu hình trang thư viện nhưng vẫn phải chèn được ảnh vào bài của mình.
    /// </remarks>
    [HttpPost("media")]
    [RequirePermission(false,
        PermissionCodes.CmsSettingManage, PermissionCodes.CmsNewsManage, PermissionCodes.CmsPageManage,
        PermissionCodes.CmsBannerManage, PermissionCodes.CmsGalleryManage)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<CmsMediaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CmsMediaDto>>> UploadMedia(
        IFormFile file, [FromQuery] string folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp."));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var result = await Mediator.Send(
            new UploadCmsMediaCommand(folder ?? "page", file.FileName, buffer.ToArray()), ct);

        return Ok(Success(result, CmsMedia.IsImage(result.ContentType) ? "Đã tải ảnh lên." : "Đã tải tệp lên."));
    }

    // ---------------------------------------------------------------
    // VIII.1 — Trang tĩnh
    // ---------------------------------------------------------------

    /// <summary>Danh sách trang tĩnh.</summary>
    [HttpGet("pages")]
    [RequirePermission(PermissionCodes.CmsPageManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CmsPageRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CmsPageRowDto>>>> Pages(
        [FromQuery] CmsPageListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsPagesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Nội dung một trang tĩnh.</summary>
    [HttpGet("pages/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsPageManage)]
    [ProducesResponseType(typeof(ApiResponse<CmsPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CmsPageDto>>> Page(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsPageQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm mới một trang tĩnh.</summary>
    [HttpPost("pages")]
    [RequirePermission(PermissionCodes.CmsPageManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePage(
        [FromBody] SaveCmsPageCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã tạo trang."));
    }

    /// <summary>Sửa một trang tĩnh.</summary>
    [HttpPut("pages/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsPageManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdatePage(
        Guid id, [FromBody] SaveCmsPageCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu trang."));
    }

    /// <summary>Xóa một trang tĩnh.</summary>
    [HttpDelete("pages/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsPageManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeletePage(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsPageCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa trang."));
    }

    // ---------------------------------------------------------------
    // VIII.2 — Tin tức
    // ---------------------------------------------------------------

    /// <summary>Danh sách tin tức.</summary>
    [HttpGet("news")]
    [RequirePermission(PermissionCodes.CmsNewsView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CmsNewsRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CmsNewsRowDto>>>> News(
        [FromQuery] CmsNewsListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsNewsListQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Nội dung một bản tin.</summary>
    [HttpGet("news/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsNewsView)]
    [ProducesResponseType(typeof(ApiResponse<CmsNewsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CmsNewsDto>>> NewsItem(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsNewsQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Soạn một bản tin mới.</summary>
    [HttpPost("news")]
    [RequirePermission(PermissionCodes.CmsNewsManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateNews(
        [FromBody] SaveCmsNewsCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu bản tin."));
    }

    /// <summary>Sửa một bản tin.</summary>
    [HttpPut("news/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsNewsManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateNews(
        Guid id, [FromBody] SaveCmsNewsCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu bản tin."));
    }

    /// <summary>Đăng hoặc gỡ một bản tin.</summary>
    [HttpPost("news/{id:guid}/publish")]
    [RequirePermission(PermissionCodes.CmsNewsPublish)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> PublishNews(
        Guid id, [FromQuery] bool publish, CancellationToken ct)
    {
        await Mediator.Send(new PublishCmsNewsCommand(id, publish), ct);
        return Ok(SuccessMessage(publish ? "Đã đăng bản tin." : "Đã gỡ bản tin."));
    }

    /// <summary>Xóa một bản tin.</summary>
    [HttpDelete("news/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsNewsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteNews(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsNewsCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa bản tin."));
    }

    /// <summary>Thống kê tin tức: số bài, lượt xem theo chuyên mục và bài xem nhiều nhất.</summary>
    [HttpGet("news/statistics")]
    [RequirePermission(PermissionCodes.CmsNewsView)]
    [ProducesResponseType(typeof(ApiResponse<CmsNewsStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CmsNewsStatisticsDto>>> NewsStatistics(
        [FromQuery] int top, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsNewsStatisticsQuery(top <= 0 ? 10 : top), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // VIII.1 — Menu điều hướng
    // ---------------------------------------------------------------

    /// <summary>Cây menu điều hướng, gồm cả mục đang tắt.</summary>
    [HttpGet("menus")]
    [RequirePermission(PermissionCodes.CmsMenuManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsMenuDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsMenuDto>>>> Menus(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsMenusQuery(IncludeInactive: true), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một mục menu.</summary>
    [HttpPost("menus")]
    [RequirePermission(PermissionCodes.CmsMenuManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateMenu(
        [FromBody] SaveCmsMenuCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm mục menu."));
    }

    /// <summary>Sửa một mục menu.</summary>
    [HttpPut("menus/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsMenuManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateMenu(
        Guid id, [FromBody] SaveCmsMenuCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu mục menu."));
    }

    /// <summary>Xóa một mục menu.</summary>
    [HttpDelete("menus/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsMenuManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteMenu(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsMenuCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa mục menu."));
    }

    /// <summary>Lưu lại thứ tự và cấp bậc sau khi kéo thả cây menu.</summary>
    [HttpPut("menus/order")]
    [RequirePermission(PermissionCodes.CmsMenuManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> ReorderMenus(
        [FromBody] List<CmsMenuPositionDto> items, CancellationToken ct)
    {
        await Mediator.Send(new ReorderCmsMenusCommand(items), ct);
        return Ok(SuccessMessage("Đã lưu thứ tự menu."));
    }

    // ---------------------------------------------------------------
    // VIII.1 — Banner
    // ---------------------------------------------------------------

    /// <summary>Danh sách banner, gồm cả banner đã hết hạn hiển thị.</summary>
    [HttpGet("banners")]
    [RequirePermission(PermissionCodes.CmsBannerManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsBannerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsBannerDto>>>> Banners(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsBannersQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một banner.</summary>
    [HttpPost("banners")]
    [RequirePermission(PermissionCodes.CmsBannerManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBanner(
        [FromBody] SaveCmsBannerCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm banner."));
    }

    /// <summary>Sửa một banner.</summary>
    [HttpPut("banners/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsBannerManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateBanner(
        Guid id, [FromBody] SaveCmsBannerCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu banner."));
    }

    /// <summary>Xóa một banner.</summary>
    [HttpDelete("banners/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsBannerManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteBanner(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsBannerCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa banner."));
    }

    // ---------------------------------------------------------------
    // VIII.1 — Liên kết website
    // ---------------------------------------------------------------

    /// <summary>Danh sách liên kết website: thư viện bạn và cơ sở dữ liệu trực tuyến.</summary>
    [HttpGet("links")]
    [RequirePermission(PermissionCodes.CmsLinkManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsExternalLinkDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsExternalLinkDto>>>> Links(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsLinksQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một liên kết website.</summary>
    [HttpPost("links")]
    [RequirePermission(PermissionCodes.CmsLinkManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLink(
        [FromBody] SaveCmsLinkCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm liên kết."));
    }

    /// <summary>Sửa một liên kết website.</summary>
    [HttpPut("links/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsLinkManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateLink(
        Guid id, [FromBody] SaveCmsLinkCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu liên kết."));
    }

    /// <summary>Xóa một liên kết website.</summary>
    [HttpDelete("links/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsLinkManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteLink(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsLinkCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa liên kết."));
    }

    // ---------------------------------------------------------------
    // VIII.2 — Thư viện ảnh
    // ---------------------------------------------------------------

    /// <summary>Danh sách album ảnh sự kiện.</summary>
    [HttpGet("galleries")]
    [RequirePermission(PermissionCodes.CmsGalleryManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CmsGalleryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CmsGalleryDto>>>> Galleries(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsGalleriesQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Tạo một album ảnh.</summary>
    [HttpPost("galleries")]
    [RequirePermission(PermissionCodes.CmsGalleryManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateGallery(
        [FromBody] SaveCmsGalleryCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã tạo album."));
    }

    /// <summary>Sửa một album ảnh.</summary>
    [HttpPut("galleries/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsGalleryManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateGallery(
        Guid id, [FromBody] SaveCmsGalleryCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu album."));
    }

    /// <summary>Xóa một album ảnh.</summary>
    [HttpDelete("galleries/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsGalleryManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteGallery(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsGalleryCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa album."));
    }

    // ---------------------------------------------------------------
    // Kiểm duyệt nhận xét bạn đọc
    // ---------------------------------------------------------------

    /// <summary>Danh sách nhận xét bạn đọc gửi lên, lọc theo tình trạng duyệt.</summary>
    [HttpGet("reviews")]
    [RequirePermission(PermissionCodes.CmsReviewModerate)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CmsReviewRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CmsReviewRowDto>>>> Reviews(
        [FromQuery] CmsReviewListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCmsReviewsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Duyệt hoặc bỏ duyệt một nhận xét.</summary>
    [HttpPost("reviews/{id:guid}/moderate")]
    [RequirePermission(PermissionCodes.CmsReviewModerate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> ModerateReview(
        Guid id, [FromQuery] bool approve, CancellationToken ct)
    {
        await Mediator.Send(new ModerateCmsReviewCommand(id, approve), ct);
        return Ok(SuccessMessage(approve ? "Đã duyệt nhận xét." : "Đã bỏ duyệt nhận xét."));
    }

    /// <summary>Xóa một nhận xét.</summary>
    [HttpDelete("reviews/{id:guid}")]
    [RequirePermission(PermissionCodes.CmsReviewModerate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteReview(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCmsReviewCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa nhận xét."));
    }
}
