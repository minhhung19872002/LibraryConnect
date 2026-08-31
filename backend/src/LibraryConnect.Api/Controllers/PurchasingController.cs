using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ III mục III.1 và III.2 — yêu cầu đặt mua, đơn đặt, biên bản bàn giao, biên mục sơ lược
/// và nhập kho.
/// </summary>
[Route("api/acquisition")]
[Tags("Bổ sung")]
public class PurchasingController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // Yêu cầu đặt mua
    // ---------------------------------------------------------------

    /// <summary>Danh sách yêu cầu đặt mua.</summary>
    [HttpGet("requests")]
    [RequirePermission(PermissionCodes.AcqRequestView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PurchaseRequestDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseRequestDto>>>> GetRequests(
        [FromQuery] PurchaseRequestListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchPurchaseRequestsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một yêu cầu đặt mua kèm các dòng tài liệu.</summary>
    [HttpGet("requests/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqRequestView)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> GetRequest(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPurchaseRequestQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Tạo yêu cầu đặt mua.</summary>
    [HttpPost("requests")]
    [RequirePermission(PermissionCodes.AcqRequestCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRequest(
        [FromBody] SavePurchaseRequestCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu yêu cầu đặt mua."));
    }

    /// <summary>Sửa yêu cầu đặt mua khi còn ở trạng thái nháp.</summary>
    [HttpPut("requests/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqRequestUpdate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateRequest(
        Guid id, [FromBody] SavePurchaseRequestCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu yêu cầu đặt mua."));
    }

    /// <summary>Xóa yêu cầu đặt mua.</summary>
    [HttpDelete("requests/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqRequestDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> DeleteRequest(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeletePurchaseRequestCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa yêu cầu đặt mua."));
    }

    /// <summary>Gửi yêu cầu đi duyệt.</summary>
    [HttpPost("requests/{id:guid}/submit")]
    [RequirePermission(PermissionCodes.AcqRequestSubmit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> SubmitRequest(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new SubmitPurchaseRequestCommand(id), ct);
        return Ok(SuccessMessage("Đã gửi yêu cầu đi duyệt."));
    }

    /// <summary>Duyệt yêu cầu — toàn bộ hoặc sửa số lượng từng dòng.</summary>
    [HttpPost("requests/{id:guid}/approve")]
    [RequirePermission(PermissionCodes.AcqRequestApprove)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestStatus>>> ApproveRequest(
        Guid id, [FromBody] ApprovePurchaseRequestCommand command, CancellationToken ct)
    {
        command.Id = id;
        var status = await Mediator.Send(command, ct);

        return Ok(Success(status, status == PurchaseRequestStatus.Submitted
            ? "Đã duyệt ở cấp này, yêu cầu chuyển lên cấp duyệt tiếp theo."
            : $"Yêu cầu chuyển sang trạng thái {PurchaseRequestStatusLabels.Of(status)}."));
    }

    /// <summary>Từ chối yêu cầu kèm lý do.</summary>
    [HttpPost("requests/{id:guid}/reject")]
    [RequirePermission(PermissionCodes.AcqRequestApprove)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> RejectRequest(
        Guid id, [FromBody] RejectRequestBody body, CancellationToken ct)
    {
        await Mediator.Send(new RejectPurchaseRequestCommand(id, body.Reason), ct);
        return Ok(SuccessMessage("Đã từ chối yêu cầu."));
    }

    /// <summary>Tra nhanh xem thư viện đã có tài liệu này chưa.</summary>
    [HttpGet("requests/duplicate-check")]
    [RequirePermission(PermissionCodes.AcqRequestView)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseDuplicateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PurchaseDuplicateDto?>>> CheckDuplicate(
        [FromQuery] string? isbn, [FromQuery] string? title, CancellationToken ct)
    {
        var result = await Mediator.Send(new CheckPurchaseDuplicateQuery(isbn, title), ct);
        return Ok(Success(result));
    }

    /// <summary>Tải tệp Excel mẫu để nhập danh sách đề nghị mua.</summary>
    [HttpGet("requests/excel-template")]
    [RequirePermission(PermissionCodes.AcqRequestImport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequestTemplate(CancellationToken ct)
    {
        var file = await Mediator.Send(new GetPurchaseRequestTemplateQuery(), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Nhập danh sách đề nghị mua từ Excel vào một yêu cầu.</summary>
    [HttpPost("requests/import")]
    [RequirePermission(PermissionCodes.AcqRequestImport)]
    [ProducesResponseType(typeof(ApiResponse<ImportPurchaseLinesResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportPurchaseLinesResultDto>>> ImportRequestLines(
        IFormFile file, [FromForm] Guid? requestId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp Excel."));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var result = await Mediator.Send(
            new ImportPurchaseRequestLinesCommand(requestId, buffer.ToArray()), ct);

        return Ok(Success(result, $"Đã đọc {result.Imported} dòng đề nghị."));
    }

    // ---------------------------------------------------------------
    // Đơn đặt
    // ---------------------------------------------------------------

    /// <summary>Danh sách đơn đặt.</summary>
    [HttpGet("orders")]
    [RequirePermission(PermissionCodes.AcqOrderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PurchaseOrderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderDto>>>> GetOrders(
        [FromQuery] PurchaseOrderListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchPurchaseOrdersQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một đơn đặt.</summary>
    [HttpGet("orders/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqOrderView)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPurchaseOrderQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Lập đơn đặt từ các yêu cầu đã duyệt, nhóm theo nhà cung cấp.</summary>
    [HttpPost("orders/from-requests")]
    [RequirePermission(PermissionCodes.AcqOrderCreate)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Guid>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Guid>>>> CreateOrdersFromRequests(
        [FromBody] CreateOrdersFromRequestsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã lập {result.Count} đơn đặt."));
    }

    /// <summary>Lập đơn đặt bằng tay.</summary>
    [HttpPost("orders")]
    [RequirePermission(PermissionCodes.AcqOrderCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateOrder(
        [FromBody] SavePurchaseOrderCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lập đơn đặt."));
    }

    /// <summary>Sửa đơn đặt khi chưa ghi nhận giao hàng.</summary>
    [HttpPut("orders/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqOrderUpdate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateOrder(
        Guid id, [FromBody] SavePurchaseOrderCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu đơn đặt."));
    }

    /// <summary>Đánh dấu đơn đã gửi nhà cung cấp, hoặc hủy đơn.</summary>
    [HttpPost("orders/{id:guid}/status")]
    [RequirePermission(PermissionCodes.AcqOrderApprove)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> SetOrderStatus(
        Guid id, [FromBody] SetOrderStatusBody body, CancellationToken ct)
    {
        await Mediator.Send(new SetPurchaseOrderStatusCommand(id, body.Status, body.Reason), ct);
        return Ok(SuccessMessage("Đã cập nhật trạng thái đơn đặt."));
    }

    /// <summary>Ghi nhận giao hàng, nhận từng phần được.</summary>
    [HttpPost("orders/{id:guid}/receive")]
    [RequirePermission(PermissionCodes.AcqOrderReceive)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderStatus>>> ReceiveOrder(
        Guid id, [FromBody] ReceiveOrderCommand command, CancellationToken ct)
    {
        command.OrderId = id;
        var status = await Mediator.Send(command, ct);
        return Ok(Success(status, "Đã ghi nhận giao hàng."));
    }

    /// <summary>Tạo ĐKCB cho các dòng đã nhận của đơn đặt.</summary>
    [HttpPost("orders/{id:guid}/create-items")]
    [RequirePermission(PermissionCodes.AcqItemCreate)]
    [ProducesResponseType(typeof(ApiResponse<CreateItemsFromOrderResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CreateItemsFromOrderResultDto>>> CreateItemsFromOrder(
        Guid id, [FromBody] CreateItemsFromOrderCommand command, CancellationToken ct)
    {
        command.OrderId = id;
        var result = await Mediator.Send(command, ct);

        var message = result.PendingCataloging.Count == 0
            ? $"Đã tạo {result.CreatedItems} ĐKCB."
            : $"Đã tạo {result.CreatedItems} ĐKCB. Còn {result.PendingCataloging.Count} dòng chưa biên mục.";

        return Ok(Success(result, message));
    }

    // ---------------------------------------------------------------
    // Biên mục sơ lược (III.2)
    // ---------------------------------------------------------------

    /// <summary>Biên mục sơ lược: form rút gọn nhưng lưu đúng cấu trúc MARC 21.</summary>
    [HttpPost("quick-catalog")]
    [RequirePermission(PermissionCodes.AcqItemCreate)]
    [ProducesResponseType(typeof(ApiResponse<QuickCatalogResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<QuickCatalogResultDto>>> QuickCatalog(
        [FromBody] QuickCatalogCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        var message = result.ReusedExisting
            ? $"Thư viện đã có biểu ghi này ({result.ControlNumber}), hệ thống dùng lại và thêm bản mới."
            : $"Đã tạo biểu ghi {result.ControlNumber} và đưa vào hàng đợi biên mục chi tiết.";

        return Ok(Success(result, message));
    }

    // ---------------------------------------------------------------
    // Biên bản bàn giao
    // ---------------------------------------------------------------

    /// <summary>Danh sách biên bản bàn giao.</summary>
    [HttpGet("handovers")]
    [RequirePermission(PermissionCodes.AcqHandoverView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HandoverDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<HandoverDto>>>> GetHandovers(
        [FromQuery] HandoverListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchHandoversQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một biên bản bàn giao.</summary>
    [HttpGet("handovers/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqHandoverView)]
    [ProducesResponseType(typeof(ApiResponse<HandoverDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HandoverDto>>> GetHandover(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetHandoverQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Lập biên bản bàn giao.</summary>
    [HttpPost("handovers")]
    [RequirePermission(PermissionCodes.AcqHandoverManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateHandover(
        [FromBody] SaveHandoverCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lập biên bản bàn giao."));
    }

    /// <summary>Sửa biên bản bàn giao.</summary>
    [HttpPut("handovers/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqHandoverManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateHandover(
        Guid id, [FromBody] SaveHandoverCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu biên bản bàn giao."));
    }

    /// <summary>Xóa biên bản bàn giao.</summary>
    [HttpDelete("handovers/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqHandoverManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteHandover(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteHandoverCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa biên bản bàn giao."));
    }

    /// <summary>Đính kèm bản scan biên bản đã ký.</summary>
    [HttpPost("handovers/{id:guid}/scan")]
    [RequirePermission(PermissionCodes.AcqHandoverManage)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<string>>> AttachHandoverScan(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp scan."));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var objectName = await Mediator.Send(
            new AttachHandoverScanCommand(id, file.ContentType, buffer.ToArray()), ct);

        return Ok(Success(objectName, "Đã đính kèm bản scan."));
    }

    /// <summary>Tải bản scan đã đính kèm.</summary>
    [HttpGet("handovers/{id:guid}/scan")]
    [RequirePermission(PermissionCodes.AcqHandoverView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHandoverScan(Guid id, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetHandoverScanQuery(id), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}

public class RejectRequestBody
{
    public string Reason { get; set; } = string.Empty;
}

public class SetOrderStatusBody
{
    public PurchaseOrderStatus Status { get; set; }
    public string? Reason { get; set; }
}
