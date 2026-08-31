using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Acq;

/// <summary>A physical library / branch. Seeded from configuration, never hardcoded.</summary>
public class Library : CatalogEntity
{
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Manager { get; set; }
    public string? OpeningHours { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsHeadquarters { get; set; }

    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}

/// <summary>Kho — a stack, reading room or discard store inside a library.</summary>
public class Warehouse : CatalogEntity
{
    public Guid LibraryId { get; set; }
    public Library? Library { get; set; }
    public WarehouseType Type { get; set; } = WarehouseType.OpenStack;
    public int? Capacity { get; set; }
    public string? Location { get; set; }
    /// <summary>
    /// Quy tắc ký hiệu xếp giá riêng của kho, ví dụ <c>{DDC} {AUTHOR:3}</c>. Bỏ trống thì dùng quy
    /// tắc chung ở tham số CATALOG.CALL_NUMBER_PATTERN. Xem CallNumberBuilder để biết các ô thay thế.
    /// </summary>
    public string? CallNumberRule { get; set; }
    /// <summary>Set while a stocktake is running: circulation is blocked for this warehouse.</summary>
    public bool IsClosedForInventory { get; set; }

    public ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();
}

/// <summary>Giá / ngăn.</summary>
public class Shelf : CatalogEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public int? Capacity { get; set; }
    public int CurrentCount { get; set; }
    /// <summary>Row/column on the stack map so the UI can draw the floor plan.</summary>
    public int? MapRow { get; set; }
    public int? MapColumn { get; set; }
    public string? CallNumberFrom { get; set; }
    public string? CallNumberTo { get; set; }
}
