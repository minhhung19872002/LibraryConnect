using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Acq;

/// <summary>
/// Kỳ kiểm kê (III.4). Creating a period snapshots the copies expected in the warehouse; scanning
/// then produces one result row per expected and per unexpected copy.
/// </summary>
public class InventoryPeriod : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    /// <summary>ALL | RANGE | DOCTYPE — how the expected set was selected.</summary>
    public string ScopeType { get; set; } = "ALL";
    public string? ScopeFrom { get; set; }
    public string? ScopeTo { get; set; }
    public Guid? ScopeDocumentTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public InventoryPeriodStatus Status { get; set; } = InventoryPeriodStatus.Preparing;
    public string? AssignedStaff { get; set; }
    public int ExpectedCount { get; set; }
    public int ScannedCount { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<InventoryScan> Scans { get; set; } = new List<InventoryScan>();
    public ICollection<InventoryResult> Results { get; set; } = new List<InventoryResult>();
}

public class InventoryScan : BaseEntity
{
    public Guid PeriodId { get; set; }
    public InventoryPeriod? Period { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; }
    public Guid? ScannedBy { get; set; }
    /// <summary>Web | Mobile | Offline reader import.</summary>
    public string? Device { get; set; }
    public InventoryResultType Outcome { get; set; }
}

public class InventoryResult : BaseEntity
{
    public Guid PeriodId { get; set; }
    public InventoryPeriod? Period { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public ItemStatus? ExpectedStatus { get; set; }
    public ItemStatus? ActualStatus { get; set; }
    public Guid? ExpectedWarehouseId { get; set; }
    public Guid? ActualWarehouseId { get; set; }
    public InventoryResultType Result { get; set; }
    public string? Note { get; set; }
    /// <summary>Set once a missing copy has been turned into a loss or disposal decision.</summary>
    public bool IsResolved { get; set; }
}

/// <summary>
/// Reusable print-form designer output (III.6): goods-receipt notes, handover minutes, transfer
/// slips, stocktake minutes, disposal decisions, loan and return slips.
/// </summary>
public class FormTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Which business document this template renders, e.g. HANDOVER, TRANSFER, LOAN_SLIP.</summary>
    public string FormType { get; set; } = string.Empty;
    /// <summary>A4 | A5 | CUSTOM.</summary>
    public string PaperSize { get; set; } = "A4";
    public bool IsLandscape { get; set; }
    public double? CustomWidthMm { get; set; }
    public double? CustomHeightMm { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Designer output: header, footer, logo, bound fields and table columns.</summary>
    public string Layout { get; set; } = "{}";
}
