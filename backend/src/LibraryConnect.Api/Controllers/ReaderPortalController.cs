using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Auth;
using LibraryConnect.Application.Features.Circulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Nhóm endpoint dành cho bạn đọc (mục XI.4).
///
/// Đây là hợp đồng API giữa máy chủ và hai phía dùng nó: trang tra cứu công khai và ứng dụng di động
/// đợt sau. Danh tính lấy từ mã thông báo của bạn đọc, không nhận mã bạn đọc từ phía gọi — nếu không
/// thì ai cũng xem được sách người khác đang mượn.
/// </summary>
[Route("api/reader")]
[Tags("Bạn đọc — API cho ứng dụng khách")]
public class ReaderPortalController : ApiControllerBase
{
    /// <summary>Đăng nhập bằng số thẻ và mật khẩu.</summary>
    [HttpPost("auth/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login(
        [FromBody] ReaderLoginCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Xin chào {result.User.FullName}."));
    }

    /// <summary>Đổi mật khẩu.</summary>
    [HttpPost("auth/change-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> ChangePassword(
        [FromBody] ChangeReaderPasswordCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đã đổi mật khẩu."));
    }

    /// <summary>Thẻ thư viện điện tử: số thẻ, hạn thẻ và chuỗi để sinh mã vạch / mã QR.</summary>
    [HttpGet("card")]
    [ProducesResponseType(typeof(ApiResponse<ReaderCardInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderCardInfoDto>>> GetCard(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyCardQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Sách đang mượn kèm hạn trả.</summary>
    [HttpGet("loans/current")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LoanRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<LoanRowDto>>>> CurrentLoans(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyLoansQuery(true, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Lịch sử mượn trả.</summary>
    [HttpGet("loans/history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LoanRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<LoanRowDto>>>> LoanHistory(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyLoansQuery(false, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Gia hạn một tài liệu đang mượn.</summary>
    [HttpPost("loans/{id:guid}/renew")]
    [ProducesResponseType(typeof(ApiResponse<LoanRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanRowDto>>> Renew(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new RenewMyLoanCommand(id), ct);
        return Ok(Success(result, $"Đã gia hạn tới ngày {result.DueDate:dd/MM/yyyy}."));
    }

    /// <summary>Mượn tự phục vụ: quét mã vạch tài liệu và mã điểm mượn tại kho.</summary>
    [HttpPost("loans/self-checkout")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CheckoutResultDto>>> SelfCheckout(
        [FromBody] SelfCheckoutCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã mượn {result.Loans.Count} tài liệu."));
    }

    /// <summary>Danh sách đặt giữ của bạn đọc.</summary>
    [HttpGet("holds")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HoldRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<HoldRowDto>>>> Holds(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyHoldsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Đặt giữ một tài liệu.</summary>
    [HttpPost("holds")]
    [ProducesResponseType(typeof(ApiResponse<HoldRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<HoldRowDto>>> PlaceHold(
        [FromBody] PlaceMyHoldCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.QueuePosition <= 1
            ? "Đã đặt giữ, bạn đang đứng đầu hàng đợi."
            : $"Đã đặt giữ, bạn đứng thứ {result.QueuePosition} trong hàng đợi."));
    }

    /// <summary>Hủy một phiếu đặt giữ của chính mình.</summary>
    [HttpDelete("holds/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CancelHold(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new CancelMyHoldCommand(id), ct);
        return Ok(SuccessMessage("Đã hủy đặt giữ."));
    }

    /// <summary>Tiền phạt và tình trạng thanh toán.</summary>
    [HttpGet("fines")]
    [ProducesResponseType(typeof(ApiResponse<ReaderFineSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderFineSummaryDto>>> Fines(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyFinesQuery(request), ct);
        return Ok(Success(result));
    }
}
