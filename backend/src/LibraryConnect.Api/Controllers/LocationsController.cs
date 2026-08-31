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
}
