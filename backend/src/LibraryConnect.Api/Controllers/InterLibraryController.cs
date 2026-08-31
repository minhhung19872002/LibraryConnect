using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.InterLibrary;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Liên thư viện (mục 3.3, 3.4 và II.7): khai báo máy chủ thư viện bạn, tra cứu sang đó, và quản
/// lý các kho OAI-PMH thu hoạch định kỳ.
/// </summary>
[Route("api/interlibrary")]
[Tags("Liên thư viện")]
public class InterLibraryController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // Máy chủ Z39.50 / SRU
    // ---------------------------------------------------------------

    /// <summary>Danh sách máy chủ thư viện bạn.</summary>
    [HttpGet("targets")]
    [RequirePermission(PermissionCodes.CatalogZ3950Search)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Z3950TargetDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Z3950TargetDto>>>> Targets(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetZ3950TargetsQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một máy chủ thư viện bạn.</summary>
    [HttpPost("targets")]
    [RequirePermission(PermissionCodes.CatalogZ3950TargetManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTarget(
        [FromBody] SaveZ3950TargetCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm máy chủ."));
    }

    /// <summary>Sửa một máy chủ thư viện bạn.</summary>
    [HttpPut("targets/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogZ3950TargetManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateTarget(
        Guid id, [FromBody] SaveZ3950TargetCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu máy chủ."));
    }

    /// <summary>Xóa một máy chủ thư viện bạn.</summary>
    [HttpDelete("targets/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogZ3950TargetManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteTarget(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteZ3950TargetCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa máy chủ."));
    }

    /// <summary>Kiểm tra kết nối tới một máy chủ: bắt tay và tra thử một từ khóa.</summary>
    [HttpPost("targets/{id:guid}/check")]
    [RequirePermission(PermissionCodes.CatalogZ3950TargetManage)]
    [ProducesResponseType(typeof(ApiResponse<Z3950CheckResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Z3950CheckResultDto>>> CheckTarget(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new CheckZ3950TargetCommand(id), ct);
        return Ok(Success(result, result.Message));
    }

    /// <summary>Tra cứu song song nhiều máy chủ thư viện bạn (II.7).</summary>
    [HttpPost("search")]
    [RequirePermission(PermissionCodes.CatalogZ3950Search)]
    [ProducesResponseType(typeof(ApiResponse<RemoteSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RemoteSearchResultDto>>> Search(
        [FromBody] RemoteSearchCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            $"Tra {result.Targets.Count} máy chủ, lấy về {result.TotalRecords} biểu ghi."));
    }

    /// <summary>
    /// Chuẩn bị một biểu ghi lấy về để mở trong trình soạn MARC.
    ///
    /// Trả về biểu ghi đã ghi nguồn và bỏ số kiểm soát của thư viện bạn, chưa lưu vào kho —
    /// cán bộ hiệu đính rồi mới bấm lưu ở màn hình biên mục.
    /// </summary>
    [HttpPost("targets/{id:guid}/prepare")]
    [RequirePermission(PermissionCodes.CatalogBibCreate)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> PrepareRecord(
        Guid id, [FromBody] PrepareRemoteRecordRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new PrepareRemoteRecordCommand(id, body.MarcJson ?? string.Empty), ct);

        return Ok(Success(result, "Đã chuẩn bị biểu ghi, hãy hiệu đính trước khi lưu."));
    }

    /// <summary>Nhật ký tra cứu liên thư viện.</summary>
    [HttpPost("search-logs")]
    [RequirePermission(PermissionCodes.CatalogZ3950Search)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Z3950SearchLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<Z3950SearchLogDto>>>> SearchLogs(
        [FromBody] Z3950SearchLogQueryRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchZ3950LogsQuery(request), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // Kho OAI-PMH
    // ---------------------------------------------------------------

    /// <summary>Danh sách kho OAI-PMH.</summary>
    [HttpGet("oai/repositories")]
    [RequirePermission(PermissionCodes.CatalogOaiManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OaiRepositoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OaiRepositoryDto>>>> Repositories(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOaiRepositoriesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một kho OAI-PMH.</summary>
    [HttpPost("oai/repositories")]
    [RequirePermission(PermissionCodes.CatalogOaiManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRepository(
        [FromBody] SaveOaiRepositoryCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm kho OAI-PMH."));
    }

    /// <summary>Sửa một kho OAI-PMH.</summary>
    [HttpPut("oai/repositories/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogOaiManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateRepository(
        Guid id, [FromBody] SaveOaiRepositoryCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu kho OAI-PMH."));
    }

    /// <summary>Xóa một kho OAI-PMH.</summary>
    [HttpDelete("oai/repositories/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogOaiManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteRepository(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteOaiRepositoryCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa kho OAI-PMH."));
    }

    /// <summary>Hỏi một kho OAI-PMH xem nó tự khai những gì.</summary>
    [HttpGet("oai/identify")]
    [RequirePermission(PermissionCodes.CatalogOaiManage)]
    [ProducesResponseType(typeof(ApiResponse<OaiIdentifyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OaiIdentifyDto>>> Identify(
        [FromQuery] string baseUrl, CancellationToken ct)
    {
        var result = await Mediator.Send(new IdentifyOaiRepositoryQuery(baseUrl), ct);
        return Ok(Success(result, $"Kho '{result.RepositoryName}' trả lời tốt."));
    }

    /// <summary>Chạy thu hoạch ngay cho một kho.</summary>
    [HttpPost("oai/repositories/{id:guid}/harvest")]
    [RequirePermission(PermissionCodes.CatalogOaiHarvest)]
    [ProducesResponseType(typeof(ApiResponse<OaiHarvestLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OaiHarvestLogDto>>> Harvest(
        Guid id, [FromQuery] bool fullReload, CancellationToken ct)
    {
        var result = await Mediator.Send(new RunOaiHarvestCommand(id, fullReload), ct);

        return Ok(Success(result,
            $"Lấy về {result.RecordsFetched} biểu ghi, nhập được {result.RecordsImported}, "
            + $"bỏ qua {result.RecordsSkipped}."));
    }

    /// <summary>Nhật ký các lần thu hoạch.</summary>
    [HttpGet("oai/harvest-logs")]
    [RequirePermission(PermissionCodes.CatalogOaiManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OaiHarvestLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OaiHarvestLogDto>>>> HarvestLogs(
        [FromQuery] Guid? repositoryId,
        [FromQuery] PagedRequestDefault request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOaiHarvestLogsQuery(repositoryId, request), ct);
        return Ok(Success(result));
    }
}

/// <summary>Thân yêu cầu khi chuẩn bị một biểu ghi lấy từ thư viện bạn.</summary>
public class PrepareRemoteRecordRequest
{
    public string? MarcJson { get; set; }
}
