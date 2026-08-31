using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Acq;

/// <summary>
/// Ấn phẩm / ĐKCB — one physical copy. This is what gets a barcode, sits on a shelf, is lent out
/// and is counted during a stocktake.
/// </summary>
public class Item : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }

    /// <summary>Scanned at the circulation desk. Unique among non-deleted rows.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Số đăng ký cá biệt, generated from the configurable numbering rule.</summary>
    public string RegisterNumber { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? ShelfId { get; set; }
    public Shelf? Shelf { get; set; }
    public string? CallNumber { get; set; }

    public decimal Price { get; set; }
    public Guid? FundingSourceId { get; set; }
    public FundingSource? FundingSource { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.Purchase;
    public Guid? OrderId { get; set; }
    public PurchaseOrder? Order { get; set; }
    public Guid? SupplierId { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.PendingInspection;
    /// <summary>Physical condition note (Tốt / Rách bìa / Ố vàng...).</summary>
    public string? Condition { get; set; }
    /// <summary>Blocks circulation while the copy is being repaired, digitised or inspected.</summary>
    public bool IsLocked { get; set; } = true;
    public string? LockReason { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    public Guid? InspectedBy { get; set; }

    public string? VolumeNumber { get; set; }
    public int CopyNumber { get; set; } = 1;
    /// <summary>Set for copies created by binding serial issues into a volume.</summary>
    public Guid? SerialBindingId { get; set; }
    public string? Note { get; set; }

    public int LoanCount { get; set; }
    public DateTimeOffset? LastLoanAt { get; set; }

    /// <summary>A copy can circulate only when it is in stock, unlocked and not soft-deleted.</summary>
    public bool IsAvailable => !IsDeleted && !IsLocked && Status == ItemStatus.InStock;
}

/// <summary>Chuyển kho — full movement history of a copy (III.5).</summary>
public class ItemMovement : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Guid? FromShelfId { get; set; }
    public Guid? ToShelfId { get; set; }
    public DateOnly MovementDate { get; set; }
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    public Guid? PerformedBy { get; set; }
    public string? PerformedByName { get; set; }
}

/// <summary>Thanh lý / ghi mất — a copy leaves the collection with a signed decision.</summary>
public class ItemDisposal : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public DateOnly DisposalDate { get; set; }
    /// <summary>Thanh lý | Mất | Hỏng không phục hồi.</summary>
    public string DisposalType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? DecisionNo { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public decimal Value { get; set; }
}

/// <summary>Barcode label layout used when printing copy labels (III.2).</summary>
public class BarcodeTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 50;
    public double HeightMm { get; set; } = 25;
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;
    public int ColumnsPerPage { get; set; } = 4;
    public int RowsPerPage { get; set; } = 10;
    public double MarginTopMm { get; set; } = 10;
    public double MarginLeftMm { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Designer output: text boxes, their item field bindings, fonts and positions.</summary>
    public string Layout { get; set; } = "{}";
}

/// <summary>Spine label layout (nhãn gáy sách).</summary>
public class LabelTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 35;
    public double HeightMm { get; set; } = 45;
    public int ColumnsPerPage { get; set; } = 5;
    public int RowsPerPage { get; set; } = 6;
    public double MarginTopMm { get; set; } = 10;
    public double MarginLeftMm { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string Layout { get; set; } = "{}";
}
