using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Danh sách cán bộ dùng cho các ô chọn người nhận việc.
///
/// Chỉ đòi đăng nhập bằng tài khoản cán bộ, không đòi quyền quản trị người dùng: người phân công
/// việc biên mục hay việc kiểm kê không vì thế mà được xem hồ sơ tài khoản của ai.
/// </summary>
[Route("api/staff")]
[Tags("Quản trị hệ thống — Cán bộ")]
[Authorize]
public class StaffController : ApiControllerBase
{
    /// <summary>Cán bộ đang hoạt động, tối đa 200 dòng; gõ từ khóa để lọc.</summary>
    [HttpGet("options")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StaffOptionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StaffOptionDto>>>> Options(
        [FromQuery] string? keyword, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStaffOptionsQuery(keyword), ct);
        return Ok(Success(result));
    }
}
