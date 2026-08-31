using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Auth;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Digital;
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

    // ---------------------------------------------------------------
    // Tài liệu số (Phân hệ V, nhóm /api/reader/digital/*)
    // ---------------------------------------------------------------

    /// <summary>Danh sách tài liệu số bạn đọc xem được.</summary>
    [HttpPost("digital/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalDocumentRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalDocumentRowDto>>>> DigitalDocuments(
        [FromBody] DigitalDocumentQueryRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyDigitalDocumentsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một tài liệu số kèm quyền đọc của chính bạn đọc.</summary>
    [HttpGet("digital/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DigitalDocumentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalDocumentDetailDto>>> DigitalDocument(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalDocumentQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Mở trình đọc trực tuyến.</summary>
    [HttpGet("digital/{id:guid}/read")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DigitalReaderSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalReaderSessionDto>>> ReadDigital(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpenDigitalReaderQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Một trang tài liệu dưới dạng ảnh, đã đóng chữ chìm tên bạn đọc và thời điểm xem.</summary>
    [HttpGet("digital/{id:guid}/pages/{page:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReadDigitalPage(Guid id, int page, CancellationToken ct)
    {
        var file = await Mediator.Send(new ReadDigitalPageQuery(id, page), ct);
        return File(file.Content, file.ContentType);
    }

    /// <summary>Tải tài liệu số về, nếu được phép.</summary>
    [HttpGet("digital/{id:guid}/download")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDigital(Guid id, CancellationToken ct)
    {
        var file = await Mediator.Send(new DownloadDigitalDocumentQuery(id), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Gửi yêu cầu đọc một tài liệu hạn chế.</summary>
    [HttpPost("digital/{id:guid}/request")]
    [ProducesResponseType(typeof(ApiResponse<DigitalAccessRequestRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DigitalAccessRequestRowDto>>> RequestDigitalAccess(
        Guid id, [FromBody] DigitalReasonRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(new RequestDigitalAccessCommand
        {
            DocumentId = id,
            Reason = body.Reason ?? string.Empty,
        }, ct);

        return Ok(Success(result, "Đã gửi yêu cầu, thư viện sẽ phản hồi sớm."));
    }

    /// <summary>Trạng thái các yêu cầu đọc đã gửi.</summary>
    [HttpGet("digital/requests")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalAccessRequestRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalAccessRequestRowDto>>>> MyDigitalRequests(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyDigitalRequestsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Lịch sử xem và tải tài liệu số.</summary>
    [HttpGet("digital/history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalAccessLogRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalAccessLogRowDto>>>> MyDigitalHistory(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyDigitalHistoryQuery(request), ct);
        return Ok(Success(result));
    }
}
