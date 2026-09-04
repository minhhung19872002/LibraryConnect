using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Auth;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Digital;
using LibraryConnect.Application.Features.Opac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    // Cửa bạn đọc mở ra Internet và số thẻ dễ đoán: cùng hạn mức dò mật khẩu với cửa cán bộ (6.4).
    [EnableRateLimiting("login")]
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

    /// <summary>
    /// Danh sách tài liệu số bạn đọc xem được — dạng gọi đơn giản bằng tham số trên địa chỉ.
    ///
    /// Đây là lối vào mà ứng dụng khách dùng để dựng màn hình danh sách: chỉ cần từ khóa, bộ sưu tập
    /// và trang. Cần lọc sâu hơn (theo định dạng, theo khoảng ngày, tìm trong toàn văn) thì gọi
    /// <c>POST /api/reader/digital/search</c> với cùng bộ lọc như giao diện quản trị.
    /// </summary>
    [HttpGet("digital")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalDocumentRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalDocumentRowDto>>>> DigitalDocumentList(
        [FromQuery] string? keyword,
        [FromQuery] Guid? collectionId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] DateTimeOffset? updatedSince,
        CancellationToken ct)
    {
        var request = new DigitalDocumentQueryRequest
        {
            Keyword = keyword,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 20 : pageSize,
            UpdatedSince = updatedSince,
            Filter = new DigitalDocumentFilter { CollectionId = collectionId }
        };

        var result = await Mediator.Send(new GetMyDigitalDocumentsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Cây bộ sưu tập tài liệu số, để bạn đọc thu hẹp danh sách theo mảng tài liệu.
    ///
    /// Cùng dữ liệu với màn hình quản trị nhưng mở cho khách vãng lai: đây là bộ lọc chính của trang
    /// Tài liệu số, không có nó thì bạn đọc chỉ còn cách gõ đúng nhan đề mới tìm ra.
    /// </summary>
    [HttpGet("digital/collections")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DigitalCollectionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DigitalCollectionDto>>>> DigitalCollections(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalCollectionsQuery(false), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách tài liệu số bạn đọc xem được, kèm bộ lọc đầy đủ.</summary>
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

    /// <summary>
    /// Tìm một cụm từ trong văn bản của tài liệu: trả về các trang có nó kèm đoạn trích. Quyền và
    /// giới hạn xem thử kiểm y như khi mở trang. Không phân biệt hoa thường và dấu.
    /// </summary>
    [HttpGet("digital/{id:guid}/find")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DigitalTextHitDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DigitalTextHitDto>>>> FindInDigital(
        Guid id, [FromQuery] string q, CancellationToken ct)
    {
        var result = await Mediator.Send(new FindInDigitalDocumentQuery(id, q), ct);
        return Ok(Success(result));
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
    // Phase 15 — ứng dụng di động
    // ---------------------------------------------------------------

    /// <summary>
    /// Xác thực vị trí trước khi mượn tự phục vụ. Máy chủ chọn cách kiểm theo tham số
    /// <c>CIRCULATION.SELF_CHECKOUT_VERIFY_MODE</c>: nối Wi-Fi thư viện (gửi <c>ssid</c>) hoặc quét mã QR
    /// trạm dán tại kho (gửi <c>qrContent</c>). Đạt thì nhận <c>verificationToken</c> có hạn dùng để nộp
    /// kèm khi gọi <c>POST /api/reader/loans/self-checkout</c>. Không đạt thì 409 kèm mã lỗi trong
    /// <c>errors[0].code</c>: LOCATION_REQUIRED, WIFI_MISMATCH, STATION_UNKNOWN, STATION_INACTIVE.
    /// </summary>
    [HttpPost("loans/self-checkout/verify")]
    [ProducesResponseType(typeof(ApiResponse<SelfCheckoutVerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SelfCheckoutVerificationDto>>> VerifySelfCheckoutLocation(
        [FromBody] VerifySelfCheckoutLocationCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã xác thực vị trí, hãy quét mã vạch tài liệu."));
    }

    /// <summary>
    /// Xin gói tài liệu số để đọc ngoại tuyến: trả khoá AES-256-CBC, hạn dùng và địa chỉ tải tệp đã mã
    /// hoá. Chỉ cấp cho tài liệu được tải về hoặc đã được duyệt quyền; tài liệu chỉ đọc trực tuyến thì 403.
    /// </summary>
    [HttpPost("digital/{id:guid}/offline-package")]
    [ProducesResponseType(typeof(ApiResponse<OfflinePackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OfflinePackageDto>>> CreateOfflinePackage(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateOfflinePackageCommand(id), ct);
        return Ok(Success(result, $"Đã cấp gói đọc ngoại tuyến, dùng được tới {result.ExpiresAt:dd/MM/yyyy}."));
    }

    /// <summary>Các gói ngoại tuyến đã cấp cho bạn đọc, kèm trạng thái hết hạn / thu hồi.</summary>
    [HttpGet("digital/offline-packages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OfflinePackageRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OfflinePackageRowDto>>>> MyOfflinePackages(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyOfflinePackagesQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Tệp đã mã hoá của một gói ngoại tuyến. Giải mã bằng khoá và IV nhận lúc xin gói.</summary>
    [HttpGet("digital/offline-packages/{packageId:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> OfflinePackageFile(Guid packageId, CancellationToken ct)
    {
        var file = await Mediator.Send(new DownloadOfflinePackageQuery(packageId), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Bạn đọc đang bật loại thông báo nào (đẩy và email); thông báo hệ thống luôn bật.</summary>
    [HttpGet("notifications/settings")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationSettingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationSettingDto>>>> NotificationSettings(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyNotificationSettingsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Bật/tắt từng loại thông báo: <c>{ "settings": { "NEWS": false, "DUE_SOON": true } }</c>.</summary>
    [HttpPut("notifications/settings")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationSettingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationSettingDto>>>> UpdateNotificationSettings(
        [FromBody] UpdateMyNotificationSettingsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu tuỳ chọn thông báo."));
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

    // ---- Đợt hoàn thiện 04/09/2026 (mobile) ----

    /// <summary>
    /// Mục lục (bookmark PDF) của một tài liệu số, làm phẳng theo thứ tự đọc: <c>level</c> để thụt lề,
    /// <c>page</c> để nhảy trang. Quyền kiểm y như khi mở trang; mục trỏ quá phần được xem thử bị cắt.
    /// Tệp không có bookmark thì trả danh sách rỗng — ứng dụng hiện "không có mục lục", không tự đoán.
    /// </summary>
    [HttpGet("digital/{id:guid}/outline")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DigitalOutlineEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DigitalOutlineEntryDto>>>> DigitalOutline(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalOutlineQuery(id), ct);
        return Ok(Success(result));
    }
}
