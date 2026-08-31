using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Nhóm endpoint công khai dùng cho OPAC và ứng dụng di động, không cần đăng nhập.
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
}
