using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Admin.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Chuông thông báo của cán bộ: yêu cầu chờ duyệt, biểu ghi được phân công, kết quả duyệt.
///
/// Không gắn mã quyền nào — ai đăng nhập cũng chỉ đọc được thông báo gửi cho **chính mình**, điều
/// kiện ấy nằm trong câu hỏi gửi xuống cơ sở dữ liệu chứ không phải kiểm sau khi đọc lên.
/// </summary>
[Route("api/notifications")]
[Tags("Quản trị hệ thống — Thông báo")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    /// <summary>Thông báo của chính người đang đăng nhập, mới nhất trước, kèm số chưa đọc.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<StaffNotificationPage>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StaffNotificationPage>>> GetMine(
        [FromQuery] PagedRequestDefault request, [FromQuery] bool unreadOnly, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStaffNotificationsQuery(request, unreadOnly), ct);
        return Ok(Success(result));
    }

    /// <summary>Đánh dấu một thông báo đã đọc.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> MarkRead(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new MarkStaffNotificationReadCommand(id), ct);
        return Ok(SuccessMessage("Đã đánh dấu thông báo là đã đọc."));
    }

    /// <summary>Đánh dấu tất cả thông báo đã đọc.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> MarkAllRead(CancellationToken ct)
    {
        await Mediator.Send(new MarkStaffNotificationReadCommand(null), ct);
        return Ok(SuccessMessage("Đã đánh dấu tất cả thông báo là đã đọc."));
    }
}
