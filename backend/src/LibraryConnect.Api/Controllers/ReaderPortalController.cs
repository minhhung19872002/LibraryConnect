using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Auth;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Opac;
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

    /// <summary>Cấp lại mã truy cập từ mã làm mới còn hiệu lực.</summary>
    /// <remarks>
    /// Cùng cơ chế với đăng nhập của cán bộ, nhưng có địa chỉ riêng dưới nhóm bạn đọc để ứng dụng
    /// di động chỉ cần biết một tiền tố duy nhất (mục XI.4).
    /// </remarks>
    [HttpPost("auth/refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Refresh(
        [FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result));
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

    // ---------------------------------------------------------------
    // Hồ sơ và thẻ
    // ---------------------------------------------------------------

    /// <summary>Hồ sơ bạn đọc đang đăng nhập.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<ReaderProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderProfileDto>>> Profile(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyProfileQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Bạn đọc tự cập nhật thông tin liên hệ.</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateProfile(
        [FromBody] UpdateMyProfileCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đã cập nhật thông tin liên hệ."));
    }

    /// <summary>Gửi yêu cầu gia hạn thẻ thư viện.</summary>
    [HttpPost("card/renew-request")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> RequestCardRenewal(
        [FromBody] CardRenewalRequestBody body, CancellationToken ct)
    {
        var id = await Mediator.Send(new RequestCardRenewalCommand(body?.Reason), ct);
        return Ok(Success(id, "Đã gửi yêu cầu gia hạn thẻ."));
    }

    /// <summary>Trạng thái các yêu cầu gia hạn thẻ đã gửi.</summary>
    [HttpGet("card/renew-requests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CardRenewalRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CardRenewalRowDto>>>> CardRenewals(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyCardRenewalsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lý do xin gia hạn thẻ.</summary>
    public class CardRenewalRequestBody
    {
        public string? Reason { get; set; }
    }

    // ---------------------------------------------------------------
    // Thông báo và thiết bị
    // ---------------------------------------------------------------

    /// <summary>Danh sách thông báo gửi tới bạn đọc.</summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderNotificationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderNotificationDto>>>> Notifications(
        [FromQuery] PagedRequestDefault request, [FromQuery] bool unreadOnly, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyNotificationsQuery(request, unreadOnly), ct);
        return Ok(Success(result));
    }

    /// <summary>Đánh dấu một thông báo đã đọc.</summary>
    [HttpPost("notifications/{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> MarkNotificationRead(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new MarkNotificationReadCommand(id), ct);
        return Ok(SuccessMessage("Đã đánh dấu đã đọc."));
    }

    /// <summary>Đánh dấu tất cả thông báo đã đọc.</summary>
    [HttpPost("notifications/read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> MarkAllNotificationsRead(CancellationToken ct)
    {
        await Mediator.Send(new MarkNotificationReadCommand(null), ct);
        return Ok(SuccessMessage("Đã đánh dấu tất cả là đã đọc."));
    }

    /// <summary>Đăng ký thiết bị nhận thông báo đẩy (chuẩn bị cho ứng dụng di động đợt sau).</summary>
    [HttpPost("devices")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RegisterDevice(
        [FromBody] RegisterDeviceTokenCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đã đăng ký thiết bị nhận thông báo."));
    }

    /// <summary>Gỡ đăng ký thiết bị khi đăng xuất khỏi ứng dụng.</summary>
    [HttpDelete("devices")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RemoveDevice(
        [FromQuery] string token, CancellationToken ct)
    {
        await Mediator.Send(new RemoveDeviceTokenCommand(token), ct);
        return Ok(SuccessMessage("Đã gỡ thiết bị."));
    }

    // ---------------------------------------------------------------
    // Yêu thích, tìm kiếm đã lưu, nhận xét, giỏ tài liệu
    // ---------------------------------------------------------------

    /// <summary>Tài liệu bạn đọc đã đánh dấu yêu thích.</summary>
    [HttpGet("favorites")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacResultDto>>>> Favorites(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyFavoritesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Bật hoặc tắt đánh dấu yêu thích cho một tài liệu.</summary>
    [HttpPost("favorites/{bibId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleFavorite(Guid bibId, CancellationToken ct)
    {
        var added = await Mediator.Send(new ToggleFavoriteCommand(bibId), ct);
        return Ok(Success(added, added ? "Đã thêm vào yêu thích." : "Đã bỏ khỏi yêu thích."));
    }

    /// <summary>Các lần tra cứu bạn đọc đã lưu lại.</summary>
    [HttpGet("saved-searches")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SavedSearchDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedSearchDto>>>> SavedSearches(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMySavedSearchesQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu lại một lần tra cứu để chạy lại sau.</summary>
    [HttpPost("saved-searches")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SaveSearch(
        [FromBody] SaveSearchCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu tìm kiếm."));
    }

    /// <summary>Xóa một tìm kiếm đã lưu.</summary>
    [HttpDelete("saved-searches/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteSavedSearch(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteSavedSearchCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa tìm kiếm đã lưu."));
    }

    /// <summary>Gửi nhận xét về một tài liệu; nhận xét chờ cán bộ duyệt mới hiện công khai.</summary>
    [HttpPost("reviews")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SubmitReview(
        [FromBody] SubmitReviewCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã gửi nhận xét, thư viện sẽ duyệt trước khi hiển thị."));
    }

    /// <summary>Gửi danh sách tài liệu trong giỏ về email đã ghi trong hồ sơ bạn đọc.</summary>
    [HttpPost("cart/email")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> EmailCart(
        [FromBody] EmailCartBody body, CancellationToken ct)
    {
        var email = await Mediator.Send(
            new EmailBibListCommand(body.BibIds ?? new List<Guid>(), body.Note), ct);

        return Ok(Success(email, $"Đã gửi danh sách tới {email}."));
    }

    /// <summary>Danh sách tài liệu cần gửi và lời nhắn kèm theo.</summary>
    public class EmailCartBody
    {
        public List<Guid>? BibIds { get; set; }
        public string? Note { get; set; }
    }
}
