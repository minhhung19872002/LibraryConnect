using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Reports;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Báo cáo thống kê toàn hệ thống.
///
/// Mỗi phân hệ đã có báo cáo riêng; nhóm này trả lời câu hỏi chung "thư viện đang thế nào" để người
/// phụ trách không phải mở bảy màn hình rồi tự cộng lại. Ai xem được báo cáo của bất kỳ phân hệ nào
/// thì xem được bảng tổng quan, vì nó chỉ gộp lại chính những con số ấy.
/// </summary>
[Route("api/reports")]
[Tags("Báo cáo thống kê")]
public class ReportsController : ApiControllerBase
{
    /// <summary>Bảng tổng quan toàn hệ thống theo khoảng thời gian.</summary>
    [HttpGet("overview")]
    [RequirePermission(
        false,
        PermissionCodes.AcqReportView,
        PermissionCodes.CirculationReportView,
        PermissionCodes.ReaderReportView,
        PermissionCodes.DigitalReportView,
        PermissionCodes.SerialReportView,
        PermissionCodes.CourseReportView)]
    [ProducesResponseType(typeof(ApiResponse<SystemOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SystemOverviewDto>>> Overview(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSystemOverviewQuery(from, to), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất bảng tổng quan ra Excel hoặc PDF.</summary>
    [HttpGet("overview/export")]
    [RequirePermission(
        false,
        PermissionCodes.AcqReportView,
        PermissionCodes.CirculationReportView,
        PermissionCodes.ReaderReportView,
        PermissionCodes.DigitalReportView,
        PermissionCodes.SerialReportView,
        PermissionCodes.CourseReportView)]
    public async Task<IActionResult> ExportOverview(
        [FromQuery] string format, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportSystemOverviewQuery(format, from, to), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
