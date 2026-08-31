using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryConnect.Api.Controllers;

/// <summary>Đăng nhập, làm mới phiên và đổi mật khẩu cho cán bộ thư viện.</summary>
[Route("api/auth")]
[Tags("Xác thực")]
public class AuthController : ApiControllerBase
{
    /// <summary>Đăng nhập bằng tên đăng nhập và mật khẩu.</summary>
    /// <remarks>
    /// Trả về access token (dùng cho header <c>Authorization: Bearer</c>) và refresh token.
    /// Nếu <c>mustChangePassword</c> = true, giao diện phải bắt buộc đổi mật khẩu trước khi vào hệ thống.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đăng nhập thành công."));
    }

    /// <summary>Cấp lại access token từ refresh token còn hiệu lực.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result));
    }

    /// <summary>Đăng xuất. Bỏ trống refreshToken để đăng xuất khỏi mọi thiết bị.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] LogoutCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đã đăng xuất."));
    }

    /// <summary>Đổi mật khẩu của chính người đang đăng nhập.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đổi mật khẩu thành công. Vui lòng đăng nhập lại."));
    }

    /// <summary>Thông tin người đang đăng nhập kèm danh sách quyền, dùng để dựng menu và ẩn/hiện nút.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<AuthUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Me(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCurrentUserQuery(), ct);
        return Ok(Success(result));
    }
}
