using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Marc;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Khổ mẫu MARC 21: bộ định nghĩa trường và kiểm tra biểu ghi (mục 3.1 và II.5).
/// </summary>
[Route("api/marc")]
[Tags("MARC 21")]
public class MarcController : ApiControllerBase
{
    /// <summary>
    /// Toàn bộ bộ định nghĩa trường. Trình soạn MARC gọi một lần khi mở màn hình để có nhãn tiếng
    /// Việt, danh sách chỉ thị và danh sách trường con của mọi trường.
    /// </summary>
    [HttpGet("fields")]
    [RequirePermission(PermissionCodes.CatalogMarcDefinitionView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarcFieldDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarcFieldDto>>>> GetFields(
        [FromQuery] string? keyword,
        [FromQuery] bool includeInactive,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMarcFieldsQuery(keyword, includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Định nghĩa của một trường theo nhãn, ví dụ 245.</summary>
    [HttpGet("fields/{tag}")]
    [RequirePermission(PermissionCodes.CatalogMarcDefinitionView)]
    [ProducesResponseType(typeof(ApiResponse<MarcFieldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MarcFieldDto>>> GetField(string tag, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMarcFieldQuery(tag), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một trường vào bộ định nghĩa, kể cả trường dùng riêng của thư viện.</summary>
    [HttpPost("fields")]
    [RequirePermission(PermissionCodes.CatalogMarcDefinitionManage)]
    [ProducesResponseType(typeof(ApiResponse<MarcFieldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MarcFieldDto>>> CreateField(
        [FromBody] SaveMarcFieldCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã thêm định nghĩa trường MARC."));
    }

    /// <summary>Sửa một trường trong bộ định nghĩa. Nhãn trường không đổi được.</summary>
    [HttpPut("fields/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogMarcDefinitionManage)]
    [ProducesResponseType(typeof(ApiResponse<MarcFieldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MarcFieldDto>>> UpdateField(
        Guid id, [FromBody] SaveMarcFieldCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật định nghĩa trường MARC."));
    }

    /// <summary>Xóa một trường khỏi bộ định nghĩa. Trường bắt buộc không xóa được.</summary>
    [HttpDelete("fields/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogMarcDefinitionManage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteField(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteMarcFieldCommand(id), ct);
        return Ok(Success<object?>(null, "Đã xóa định nghĩa trường MARC."));
    }

    /// <summary>
    /// Kiểm tra một biểu ghi đang soạn theo bộ định nghĩa hiện hành. Trả về danh sách lỗi và cảnh
    /// báo, mỗi mục chỉ đúng trường và trường con để giao diện tô sáng.
    /// </summary>
    [HttpPost("validate")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<MarcValidationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MarcValidationResultDto>>> Validate(
        [FromBody] ValidateMarcRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ValidateMarcRecordCommand(request.MarcJson), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Xem trước mô tả ISBD của biểu ghi đang soạn, chưa lưu (II.2).
    ///
    /// Không ghi gì xuống cơ sở dữ liệu: biểu ghi đi thẳng từ trình soạn lên, đọc xong trả về mô tả.
    /// </summary>
    [HttpPost("preview")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<MarcPreviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MarcPreviewDto>>> Preview(
        [FromBody] ValidateMarcRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new PreviewMarcRecordCommand(request.MarcJson), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Đọc một tệp trao đổi biểu ghi (.mrc theo ISO 2709 hoặc .xml theo MARCXML) và trả về các biểu
    /// ghi kèm kết quả kiểm tra. Không ghi gì vào cơ sở dữ liệu — dùng để xem trước trước khi nhập.
    /// </summary>
    [HttpPost("parse")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<ParseMarcFileResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ParseMarcFileResultDto>>> Parse(
        IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp biểu ghi cần đọc."));
        }

        using var buffer = new MemoryStream();
        await using (var stream = file.OpenReadStream())
        {
            await stream.CopyToAsync(buffer, ct);
        }

        var result = await Mediator.Send(new ParseMarcFileCommand(buffer.ToArray(), file.FileName), ct);

        var message = result.Errors.Count == 0
            ? $"Đọc được {result.TotalRecords} biểu ghi theo định dạng {result.Format}."
            : $"Đọc được {result.TotalRecords} biểu ghi, {result.Errors.Count} biểu ghi lỗi.";

        return Ok(Success(result, message));
    }

    /// <summary>
    /// Xuất các biểu ghi ra tệp trao đổi. Định dạng <c>iso2709</c> cho tệp .mrc,
    /// <c>marcxml</c> cho tệp .xml.
    /// </summary>
    [HttpPost("export")]
    [RequirePermission(PermissionCodes.CatalogBibExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export([FromBody] ExportMarcRequest request, CancellationToken ct)
    {
        var file = await Mediator.Send(
            new ExportMarcRecordsCommand(request.Records, request.Format, request.FileName), ct);

        return File(file.Content, file.ContentType, file.FileName);
    }
}

/// <summary>Biểu ghi cần kiểm tra, dạng JSON như lưu trong cột marc_json.</summary>
public class ValidateMarcRequest
{
    public string MarcJson { get; set; } = string.Empty;
}

/// <summary>Các biểu ghi cần xuất ra tệp trao đổi.</summary>
public class ExportMarcRequest
{
    /// <summary>Mỗi phần tử là một biểu ghi ở dạng JSON.</summary>
    public List<string> Records { get; set; } = new();

    /// <summary>iso2709 hoặc marcxml. Mặc định là iso2709.</summary>
    public string Format { get; set; } = "iso2709";

    /// <summary>Tên tệp không kèm phần mở rộng. Bỏ trống thì hệ thống tự đặt theo thời điểm xuất.</summary>
    public string? FileName { get; set; }
}
