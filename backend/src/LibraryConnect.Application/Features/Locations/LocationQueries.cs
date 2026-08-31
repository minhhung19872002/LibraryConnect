using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Locations;

public class LibraryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsHeadquarters { get; set; }
    public bool IsActive { get; set; }
}

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid LibraryId { get; set; }
    public string? LibraryName { get; set; }
    public WarehouseType Type { get; set; }
    public int? Capacity { get; set; }
    public string? Location { get; set; }
    /// <summary>Quy tắc ký hiệu xếp giá riêng của kho; bỏ trống thì dùng quy tắc chung của thư viện.</summary>
    public string? CallNumberRule { get; set; }
    public bool IsClosedForInventory { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
}

public class ShelfDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int? Capacity { get; set; }
    public int CurrentCount { get; set; }
    public int? MapRow { get; set; }
    public int? MapColumn { get; set; }
    public string? CallNumberFrom { get; set; }
    public string? CallNumberTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Danh sách thư viện, dùng cho ô chọn trên các màn hình nghiệp vụ.</summary>
public record GetLibrariesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<LibraryDto>>;

public class GetLibrariesQueryHandler : IRequestHandler<GetLibrariesQuery, IReadOnlyList<LibraryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetLibrariesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<LibraryDto>> Handle(GetLibrariesQuery query, CancellationToken ct) =>
        await _db.Libraries
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, library => library.IsActive)
            .OrderByDescending(library => library.IsHeadquarters)
            .ThenBy(library => library.SortOrder)
            .ThenBy(library => library.Name)
            .Select(library => new LibraryDto
            {
                Id = library.Id,
                Code = library.Code,
                Name = library.Name,
                Address = library.Address,
                IsHeadquarters = library.IsHeadquarters,
                IsActive = library.IsActive
            })
            .ToListAsync(ct);
}

/// <summary>Danh sách kho, lọc theo thư viện.</summary>
public record GetWarehousesQuery(Guid? LibraryId = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<WarehouseDto>>;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IApplicationDbContext _db;

    public GetWarehousesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery query, CancellationToken ct) =>
        await _db.Warehouses
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, warehouse => warehouse.IsActive)
            .WhereIf(query.LibraryId is not null, warehouse => warehouse.LibraryId == query.LibraryId)
            .OrderBy(warehouse => warehouse.SortOrder)
            .ThenBy(warehouse => warehouse.Name)
            .Select(warehouse => new WarehouseDto
            {
                Id = warehouse.Id,
                Code = warehouse.Code,
                Name = warehouse.Name,
                LibraryId = warehouse.LibraryId,
                LibraryName = warehouse.Library!.Name,
                Type = warehouse.Type,
                Capacity = warehouse.Capacity,
                Location = warehouse.Location,
                CallNumberRule = warehouse.CallNumberRule,
                IsClosedForInventory = warehouse.IsClosedForInventory,
                IsActive = warehouse.IsActive,
                ItemCount = _db.Items.Count(item => item.WarehouseId == warehouse.Id)
            })
            .ToListAsync(ct);
}

/// <summary>Danh sách giá của một kho, dùng cho ô chọn vị trí và cho bản đồ kho.</summary>
public record GetShelvesQuery(Guid? WarehouseId = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<ShelfDto>>;

public class GetShelvesQueryHandler : IRequestHandler<GetShelvesQuery, IReadOnlyList<ShelfDto>>
{
    private readonly IApplicationDbContext _db;

    public GetShelvesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ShelfDto>> Handle(GetShelvesQuery query, CancellationToken ct) =>
        await _db.Shelves
            .AsNoTracking()
            .WhereIf(!query.IncludeInactive, shelf => shelf.IsActive)
            .WhereIf(query.WarehouseId is not null, shelf => shelf.WarehouseId == query.WarehouseId)
            .OrderBy(shelf => shelf.MapRow)
            .ThenBy(shelf => shelf.MapColumn)
            .ThenBy(shelf => shelf.Name)
            .Select(shelf => new ShelfDto
            {
                Id = shelf.Id,
                Code = shelf.Code,
                Name = shelf.Name,
                WarehouseId = shelf.WarehouseId,
                WarehouseName = shelf.Warehouse!.Name,
                Capacity = shelf.Capacity,
                CurrentCount = shelf.CurrentCount,
                MapRow = shelf.MapRow,
                MapColumn = shelf.MapColumn,
                CallNumberFrom = shelf.CallNumberFrom,
                CallNumberTo = shelf.CallNumberTo,
                IsActive = shelf.IsActive
            })
            .ToListAsync(ct);
}
