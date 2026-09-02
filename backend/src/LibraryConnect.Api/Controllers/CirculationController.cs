using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Circulation;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ VII — Lưu thông: chính sách, ghi mượn, ghi trả, gia hạn, đặt giữ, tiền phạt, tủ gửi đồ,
/// ghi nhận ra vào thư viện và báo cáo.
/// </summary>
[Route("api/circulation")]
[Tags("Lưu thông")]
public class CirculationController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // VII.1 — Chính sách và lịch nghỉ
    // ---------------------------------------------------------------

    /// <summary>Ma trận chính sách lưu thông: loại bạn đọc × dạng tài liệu × kho.</summary>
    [HttpGet("policies")]
    [RequirePermission(PermissionCodes.CirculationPolicyView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CirculationPolicyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CirculationPolicyDto>>>> GetPolicies(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCirculationPoliciesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu một ô của ma trận chính sách.</summary>
    [HttpPost("policies")]
    [RequirePermission(PermissionCodes.CirculationPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SavePolicy(
        [FromBody] SaveCirculationPolicyCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu chính sách lưu thông."));
    }

    /// <summary>Xóa một chính sách.</summary>
    [HttpDelete("policies/{id:guid}")]
    [RequirePermission(PermissionCodes.CirculationPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeletePolicy(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCirculationPolicyCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa chính sách."));
    }

    /// <summary>Thử xem một cặp bạn đọc × tài liệu × kho sẽ rơi vào chính sách nào.</summary>
    [HttpGet("policies/preview")]
    [RequirePermission(PermissionCodes.CirculationPolicyView)]
    [ProducesResponseType(typeof(ApiResponse<EffectivePolicy>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EffectivePolicy>>> PreviewPolicy(
        [FromQuery] Guid? readerTypeId, [FromQuery] Guid? documentTypeId,
        [FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new PreviewPolicyQuery(readerTypeId, documentTypeId, warehouseId), ct);

        return Ok(Success(result));
    }

    /// <summary>Lịch nghỉ lễ dùng để dời hạn trả và trừ ngày phạt.</summary>
    [HttpGet("holidays")]
    [RequirePermission(PermissionCodes.CirculationPolicyView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HolidayDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HolidayDto>>>> GetHolidays(
        [FromQuery] int? year, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetHolidaysQuery(year), ct);
        return Ok(Success(result));
    }

    [HttpPost("holidays")]
    [RequirePermission(PermissionCodes.CirculationPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SaveHoliday(
        [FromBody] SaveHolidayCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu ngày nghỉ."));
    }

    [HttpDelete("holidays/{id:guid}")]
    [RequirePermission(PermissionCodes.CirculationPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteHoliday(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteHolidayCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa ngày nghỉ."));
    }

    /// <summary>Thử lịch: cho ngày mượn và số ngày mượn, xem hạn trả rơi vào ngày nào.</summary>
    [HttpGet("holidays/preview-due-date")]
    [RequirePermission(PermissionCodes.CirculationPolicyView)]
    [ProducesResponseType(typeof(ApiResponse<DueDatePreviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DueDatePreviewDto>>> PreviewDueDate(
        [FromQuery] DateOnly loanDate, [FromQuery] int loanDays, CancellationToken ct)
    {
        var result = await Mediator.Send(new PreviewDueDateQuery(loanDate, loanDays), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // VII.2 — Quầy: ghi mượn, ghi trả, gia hạn
    // ---------------------------------------------------------------

    /// <summary>
    /// Quét thẻ ở quầy. Nhận số thẻ hoặc mã sinh viên, trả về hồ sơ, cảnh báo, sách đang giữ và
    /// tài liệu đặt giữ đã sẵn sàng.
    /// </summary>
    [HttpGet("desk/reader")]
    [RequirePermission(PermissionCodes.CirculationLoanView)]
    [ProducesResponseType(typeof(ApiResponse<DeskReaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DeskReaderDto>>> GetDeskReader(
        [FromQuery] string cardNumber, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDeskReaderQuery(cardNumber), ct);
        return Ok(Success(result));
    }

    /// <summary>Làm mới thông tin quầy theo mã bạn đọc.</summary>
    [HttpGet("desk/reader/{readerId:guid}")]
    [RequirePermission(PermissionCodes.CirculationLoanView)]
    [ProducesResponseType(typeof(ApiResponse<DeskReaderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DeskReaderDto>>> GetDeskReaderById(
        Guid readerId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDeskReaderByIdQuery(readerId), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Quét một mã vạch ở màn hình ghi mượn: máy chủ kiểm tra chính sách và tính sẵn hạn trả.
    /// </summary>
    [HttpPost("desk/scan")]
    [RequirePermission(PermissionCodes.CirculationLoanCreate)]
    [ProducesResponseType(typeof(ApiResponse<ScanForLoanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ScanForLoanDto>>> Scan(
        [FromBody] ScanRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new ScanForLoanQuery(body.ReaderId, body.Barcode, body.Pending), ct);

        return Ok(Success(result));
    }

    /// <summary>Hoàn tất ghi mượn cho danh sách mã vạch đã quét.</summary>
    [HttpPost("desk/checkout")]
    [RequirePermission(PermissionCodes.CirculationLoanCreate)]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CheckoutResultDto>>> Checkout(
        [FromBody] CheckoutCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.Failures.Count == 0
            ? $"Đã ghi mượn {result.Loans.Count} tài liệu."
            : $"Đã ghi mượn {result.Loans.Count} tài liệu, {result.Failures.Count} mã vạch không ghi được."));
    }

    /// <summary>Ghi trả theo danh sách mã vạch.</summary>
    [HttpPost("desk/return")]
    [RequirePermission(PermissionCodes.CirculationLoanReturn)]
    [ProducesResponseType(typeof(ApiResponse<ReturnResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReturnResultDto>>> Return(
        [FromBody] ReturnCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.TotalFine > 0
            ? $"Đã ghi trả {result.Items.Count} tài liệu, tiền phạt {result.TotalFine:#,##0} đ."
            : $"Đã ghi trả {result.Items.Count} tài liệu."));
    }

    /// <summary>Gia hạn theo mã lượt mượn.</summary>
    [HttpPost("loans/{id:guid}/renew")]
    [RequirePermission(PermissionCodes.CirculationLoanRenew)]
    [ProducesResponseType(typeof(ApiResponse<LoanRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanRowDto>>> Renew(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new RenewLoanCommand(id), ct);
        return Ok(Success(result, $"Đã gia hạn tới ngày {result.DueDate:dd/MM/yyyy}."));
    }

    /// <summary>Gia hạn bằng cách quét mã vạch tài liệu.</summary>
    [HttpPost("desk/renew-by-barcode")]
    [RequirePermission(PermissionCodes.CirculationLoanRenew)]
    [ProducesResponseType(typeof(ApiResponse<LoanRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoanRowDto>>> RenewByBarcode(
        [FromBody] BarcodeRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(new RenewByBarcodeCommand(body.Barcode), ct);
        return Ok(Success(result, $"Đã gia hạn tới ngày {result.DueDate:dd/MM/yyyy}."));
    }

    /// <summary>Ghi nhận tài liệu mất hoặc hỏng và lập khoản bồi thường.</summary>
    [HttpPost("loans/{id:guid}/close-as-lost")]
    [RequirePermission(PermissionCodes.CirculationLoanReturn)]
    [ProducesResponseType(typeof(ApiResponse<FineRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FineRowDto>>> CloseAsLost(
        Guid id, [FromBody] CloseLoanAsLostCommand command, CancellationToken ct)
    {
        command.LoanId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, command.Damaged
            ? $"Đã ghi nhận tài liệu hỏng, bồi thường {result.Amount:#,##0} đ."
            : $"Đã ghi nhận tài liệu mất, bồi thường {result.Amount:#,##0} đ."));
    }

    /// <summary>Danh sách giao dịch mượn trả.</summary>
    [HttpGet("loans")]
    [RequirePermission(PermissionCodes.CirculationLoanView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LoanRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<LoanRowDto>>>> SearchLoans(
        [FromQuery] LoanListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchLoansQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một lượt mượn kèm lịch sử gia hạn và tiền phạt.</summary>
    [HttpGet("loans/{id:guid}")]
    [RequirePermission(PermissionCodes.CirculationLoanView)]
    [ProducesResponseType(typeof(ApiResponse<LoanDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoanDetailDto>>> GetLoan(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLoanQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Các yêu cầu gia hạn từ xa đang chờ cán bộ duyệt.</summary>
    [HttpGet("renewals/pending")]
    [RequirePermission(PermissionCodes.CirculationLoanRenew)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PendingRenewalDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingRenewalDto>>>> GetPendingRenewals(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPendingRenewalsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Duyệt hoặc từ chối một yêu cầu gia hạn.</summary>
    [HttpPost("renewals/{id:guid}/process")]
    [RequirePermission(PermissionCodes.CirculationLoanRenew)]
    [ProducesResponseType(typeof(ApiResponse<LoanRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoanRowDto>>> ProcessRenewal(
        Guid id, [FromBody] ProcessRenewalRequestCommand command, CancellationToken ct)
    {
        command.RenewalId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, command.Approve ? "Đã duyệt gia hạn." : "Đã từ chối yêu cầu gia hạn."));
    }

    // ---------------------------------------------------------------
    // VII.2 — Đặt giữ chỗ
    // ---------------------------------------------------------------

    [HttpGet("holds")]
    [RequirePermission(PermissionCodes.CirculationHoldManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HoldRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<HoldRowDto>>>> SearchHolds(
        [FromQuery] HoldListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchHoldsQuery(request), ct);
        return Ok(Success(result));
    }

    [HttpPost("holds")]
    [RequirePermission(PermissionCodes.CirculationHoldManage)]
    [ProducesResponseType(typeof(ApiResponse<HoldRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<HoldRowDto>>> PlaceHold(
        [FromBody] PlaceHoldCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.QueuePosition <= 1
            ? "Đã đặt giữ, bạn đọc đứng đầu hàng đợi."
            : $"Đã đặt giữ, bạn đọc đứng thứ {result.QueuePosition} trong hàng đợi."));
    }

    [HttpDelete("holds/{id:guid}")]
    [RequirePermission(PermissionCodes.CirculationHoldManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CancelHold(
        Guid id, [FromQuery] string? reason, CancellationToken ct)
    {
        await Mediator.Send(new CancelHoldCommand(id, reason), ct);
        return Ok(SuccessMessage("Đã hủy phiếu đặt giữ."));
    }

    /// <summary>Hàng đợi đặt giữ của một biểu ghi.</summary>
    [HttpGet("holds/queue/{bibId:guid}")]
    [RequirePermission(PermissionCodes.CirculationHoldManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HoldRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HoldRowDto>>>> GetHoldQueue(
        Guid bibId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetHoldQueueQuery(bibId), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // VII.2 — Tiền phạt
    // ---------------------------------------------------------------

    [HttpGet("fines")]
    [RequirePermission(PermissionCodes.CirculationFineView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FineRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<FineRowDto>>>> SearchFines(
        [FromQuery] FineListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchFinesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Bảng nợ của một bạn đọc, dùng ở màn hình thu tiền.</summary>
    [HttpGet("fines/reader/{readerId:guid}")]
    [RequirePermission(PermissionCodes.CirculationFineView)]
    [ProducesResponseType(typeof(ApiResponse<ReaderFineSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderFineSummaryDto>>> GetReaderFines(
        Guid readerId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderFineSummaryQuery(readerId), ct);
        return Ok(Success(result));
    }

    /// <summary>Ghi một khoản phạt thủ công.</summary>
    [HttpPost("fines")]
    [RequirePermission(PermissionCodes.CirculationFineCollect)]
    [ProducesResponseType(typeof(ApiResponse<FineRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FineRowDto>>> CreateFine(
        [FromBody] CreateFineCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã lập khoản phạt {result.Code}."));
    }

    /// <summary>Thu tiền phạt, thu được từng phần.</summary>
    [HttpPost("fines/{id:guid}/pay")]
    [RequirePermission(PermissionCodes.CirculationFineCollect)]
    [ProducesResponseType(typeof(ApiResponse<FineRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FineRowDto>>> PayFine(
        Guid id, [FromBody] PayFineCommand command, CancellationToken ct)
    {
        command.FineId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.Outstanding > 0
            ? $"Đã thu, còn nợ {result.Outstanding:#,##0} đ."
            : "Đã thu đủ tiền phạt."));
    }

    /// <summary>Miễn giảm tiền phạt, phải ghi lý do.</summary>
    [HttpPost("fines/{id:guid}/waive")]
    [RequirePermission(PermissionCodes.CirculationFineWaive)]
    [ProducesResponseType(typeof(ApiResponse<FineRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FineRowDto>>> WaiveFine(
        Guid id, [FromBody] WaiveFineCommand command, CancellationToken ct)
    {
        command.FineId = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã miễn khoản phạt."));
    }

    // ---------------------------------------------------------------
    // VII.2 — Ra vào thư viện
    // ---------------------------------------------------------------

    /// <summary>Quét thẻ tại cổng: lần quét đầu là vào, lần sau là ra.</summary>
    [HttpPost("gate/scan")]
    [RequirePermission(PermissionCodes.CirculationVisitManage)]
    [ProducesResponseType(typeof(ApiResponse<GateScanResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GateScanResultDto>>> ScanGate(
        [FromBody] ScanGateCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, result.Message));
    }

    [HttpGet("visits")]
    [RequirePermission(PermissionCodes.CirculationVisitManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VisitRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<VisitRowDto>>>> SearchVisits(
        [FromQuery] VisitListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchVisitsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Đóng các lượt vào còn bỏ ngỏ khi thư viện đóng cửa.</summary>
    [HttpPost("visits/close-open")]
    [RequirePermission(PermissionCodes.CirculationVisitManage)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> CloseOpenVisits(
        [FromQuery] DateOnly? date, CancellationToken ct)
    {
        var closed = await Mediator.Send(new CloseOpenVisitsCommand(date), ct);
        return Ok(Success(closed, $"Đã đóng {closed} lượt vào còn bỏ ngỏ."));
    }

    // ---------------------------------------------------------------
    // VII.3 — Tủ gửi đồ
    // ---------------------------------------------------------------

    [HttpGet("lockers")]
    [RequirePermission(PermissionCodes.CirculationLockerManage)]
    [ProducesResponseType(typeof(ApiResponse<LockerMapDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LockerMapDto>>> GetLockerMap(
        [FromQuery] Guid? libraryId, [FromQuery] string? area, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLockerMapQuery(libraryId, area), ct);
        return Ok(Success(result));
    }

    [HttpPost("lockers")]
    [RequirePermission(PermissionCodes.CirculationLockerManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SaveLocker(
        [FromBody] SaveLockerCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu tủ gửi đồ."));
    }

    [HttpDelete("lockers/{id:guid}")]
    [RequirePermission(PermissionCodes.CirculationLockerManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteLocker(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteLockerCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa tủ gửi đồ."));
    }

    /// <summary>Giao tủ cho bạn đọc.</summary>
    [HttpPost("lockers/{id:guid}/assign")]
    [RequirePermission(PermissionCodes.CirculationLockerManage)]
    [ProducesResponseType(typeof(ApiResponse<LockerRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LockerRowDto>>> AssignLocker(
        Guid id, [FromBody] AssignLockerCommand command, CancellationToken ct)
    {
        command.LockerId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, $"Đã giao tủ {result.Code} cho {result.ReaderName}."));
    }

    /// <summary>Trả tủ theo số tủ hoặc theo thẻ bạn đọc.</summary>
    [HttpPost("lockers/release")]
    [RequirePermission(PermissionCodes.CirculationLockerManage)]
    [ProducesResponseType(typeof(ApiResponse<LockerUsageRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LockerUsageRowDto>>> ReleaseLocker(
        [FromBody] ReleaseLockerCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã nhận lại tủ {result.LockerCode}."));
    }

    [HttpGet("lockers/usages")]
    [RequirePermission(PermissionCodes.CirculationLockerManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LockerUsageRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<LockerUsageRowDto>>>> SearchLockerUsages(
        [FromQuery] LockerUsageListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchLockerUsagesQuery(request), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // VII.5 — Bảy báo cáo
    // ---------------------------------------------------------------

    /// <summary>1. Bạn đọc ra vào thư viện, kèm biểu đồ giờ cao điểm.</summary>
    [HttpGet("reports/visits")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<VisitReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitReportDto>>> VisitReport(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetVisitReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>2. Bạn đọc đang mượn tài liệu.</summary>
    [HttpGet("reports/current-loans")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LoanRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanRowDto>>>> CurrentLoansReport(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCurrentLoansReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>3. Lịch sử mượn trả.</summary>
    [HttpGet("reports/history")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<LoanHistoryReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoanHistoryReportDto>>> HistoryReport(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLoanHistoryReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>4. Bạn đọc mượn quá hạn.</summary>
    [HttpGet("reports/overdue")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<OverdueReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OverdueReportDto>>> OverdueReport(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOverdueReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Gửi email nhắc hạn hàng loạt cho danh sách quá hạn.</summary>
    [HttpPost("reports/overdue/remind")]
    [RequirePermission(PermissionCodes.CirculationLoanView)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> SendOverdueReminders(
        [FromBody] SendOverdueRemindersCommand command, CancellationToken ct)
    {
        var sent = await Mediator.Send(command, ct);
        return Ok(Success(sent, $"Đã gửi nhắc tới {sent} bạn đọc."));
    }

    /// <summary>5. Sử dụng tủ đựng đồ.</summary>
    [HttpGet("reports/lockers")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<LockerReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LockerReportDto>>> LockerReport(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLockerReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>6. Bạn đọc mượn tài liệu nhiều nhất.</summary>
    [HttpGet("reports/top-readers")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopReaderRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopReaderRowDto>>>> TopReaders(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetTopReadersReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>7. Ấn phẩm được mượn nhiều nhất.</summary>
    [HttpGet("reports/top-items")]
    [RequirePermission(PermissionCodes.CirculationReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopItemRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopItemRowDto>>>> TopItems(
        [FromQuery] CirculationReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetTopItemsReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất báo cáo đang xem ra Excel hoặc PDF.</summary>
    [HttpPost("reports/export")]
    [RequirePermission(PermissionCodes.CirculationReportExport)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromBody] ExportCirculationReportQuery query, CancellationToken ct)
    {
        var file = await Mediator.Send(query, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------
    // Trạm mượn tự phục vụ (Phase 15, mục 3.2)
    // ---------------------------------------------------------------

    /// <summary>Danh sách trạm mượn tự phục vụ kèm nội dung mã QR để in.</summary>
    [HttpGet("stations")]
    [RequirePermission(PermissionCodes.CirculationPolicyView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CheckoutStationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CheckoutStationDto>>>> Stations(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCheckoutStationsQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm hoặc sửa một trạm (có <c>id</c> là sửa).</summary>
    [HttpPost("stations")]
    [RequirePermission(PermissionCodes.CirculationPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse<CheckoutStationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CheckoutStationDto>>> SaveStation(
        [FromBody] SaveCheckoutStationCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã lưu trạm {result.Code}."));
    }

    [HttpDelete("stations/{id:guid}")]
    [RequirePermission(PermissionCodes.CirculationPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteStation(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCheckoutStationCommand(id), ct);
        return Ok(SuccessMessage("Đã xoá trạm."));
    }

    /// <summary>Ảnh PNG mã QR của một trạm để in và dán tại kho.</summary>
    [HttpGet("stations/{id:guid}/qr.png")]
    [RequirePermission(PermissionCodes.CirculationPolicyView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StationQr(Guid id, [FromQuery] int size, CancellationToken ct)
    {
        var stations = await Mediator.Send(new GetCheckoutStationsQuery(true), ct);
        var station = stations.FirstOrDefault(row => row.Id == id)
                      ?? throw new LibraryConnect.Application.Common.Exceptions.NotFoundException("trạm mượn", id);

        var pixels = size is < 120 or > 1200 ? 480 : size;
        var png = LibraryConnect.Reporting.Barcodes.BarcodeRenderer.Render(
            station.QrContent, LibraryConnect.Domain.Enums.BarcodeType.QrCode, pixels, pixels);

        return File(png, "image/png", $"tram-{station.Code}.png");
    }
}

public class ScanRequest
{
    public Guid ReaderId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Các mã vạch đã có trong danh sách đang ghi mượn, để bắt lỗi quét trùng.</summary>
    public List<string>? Pending { get; set; }
}

public class BarcodeRequest
{
    public string Barcode { get; set; } = string.Empty;
}
