using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Locations;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Thư viện, kho và giá — dữ liệu vị trí dùng chung cho biên mục, bổ sung và lưu thông.
/// </summary>
[Route("api/locations")]
[Tags("Kho và giá")]
public class LocationsController : ApiControllerBase
{
    /// <summary>Danh sách thư viện.</summary>
    [HttpGet("libraries")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LibraryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LibraryDto>>>> GetLibraries(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLibrariesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách kho, lọc theo thư viện.</summary>
    [HttpGet("warehouses")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseDto>>>> GetWarehouses(
        [FromQuery] Guid? libraryId, [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetWarehousesQuery(libraryId, includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách giá của một kho.</summary>
    [HttpGet("shelves")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShelfDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShelfDto>>>> GetShelves(
        [FromQuery] Guid? warehouseId, [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetShelvesQuery(warehouseId, includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một thư viện / cơ sở.</summary>
    [HttpGet("libraries/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqWarehouseView)]
    [ProducesResponseType(typeof(ApiResponse<LibraryDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LibraryDetailDto>>> GetLibrary(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLibraryQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm thư viện / cơ sở.</summary>
    [HttpPost("libraries")]
    [RequirePermission(PermissionCodes.AcqLibraryManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLibrary(
        [FromBody] SaveLibraryCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm thư viện."));
    }

    /// <summary>Sửa thư viện / cơ sở.</summary>
    [HttpPut("libraries/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqLibraryManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateLibrary(
        Guid id, [FromBody] SaveLibraryCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu thư viện."));
    }

    /// <summary>Xóa thư viện / cơ sở khi không còn kho nào thuộc về nó.</summary>
    [HttpDelete("libraries/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqLibraryManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> DeleteLibrary(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteLibraryCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa thư viện."));
    }

    /// <summary>Chi tiết một kho.</summary>
    [HttpGet("warehouses/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqWarehouseView)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WarehouseDetailDto>>> GetWarehouse(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetWarehouseQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm kho.</summary>
    [HttpPost("warehouses")]
    [RequirePermission(PermissionCodes.AcqWarehouseManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateWarehouse(
        [FromBody] SaveWarehouseCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm kho."));
    }

    /// <summary>Sửa kho.</summary>
    [HttpPut("warehouses/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqWarehouseManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateWarehouse(
        Guid id, [FromBody] SaveWarehouseCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu kho."));
    }

    /// <summary>Xóa kho khi không còn ấn phẩm nào trong kho.</summary>
    [HttpDelete("warehouses/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqWarehouseManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> DeleteWarehouse(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteWarehouseCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa kho."));
    }

    /// <summary>Bản đồ kho: lưới giá kèm mức lấp đầy của từng giá.</summary>
    [HttpGet("warehouses/{id:guid}/map")]
    [RequirePermission(PermissionCodes.AcqWarehouseView)]
    [ProducesResponseType(typeof(ApiResponse<ShelfMapDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ShelfMapDto>>> GetShelfMap(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetShelfMapQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm giá.</summary>
    [HttpPost("shelves")]
    [RequirePermission(PermissionCodes.AcqWarehouseManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateShelf(
        [FromBody] SaveShelfCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm giá."));
    }

    /// <summary>Sửa giá.</summary>
    [HttpPut("shelves/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqWarehouseManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateShelf(
        Guid id, [FromBody] SaveShelfCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu giá."));
    }

    /// <summary>Xóa giá khi trên giá không còn ấn phẩm.</summary>
    [HttpDelete("shelves/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqWarehouseManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> DeleteShelf(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteShelfCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa giá."));
    }
}
