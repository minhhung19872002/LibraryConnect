using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Cir;

/// <summary>
/// One cell of the circulation matrix (VII.1): reader type × document type × warehouse. A null
/// dimension means "any". When several policies match, the one with the highest
/// <see cref="Priority"/> wins.
/// </summary>
public class CirculationPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ReaderTypeId { get; set; }
    public ReaderType? ReaderType { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int MaxItems { get; set; } = 5;
    public int LoanDays { get; set; } = 14;
    public int MaxRenewals { get; set; } = 2;
    public int RenewalDays { get; set; } = 7;
    public decimal FinePerDay { get; set; }
    public int GraceDays { get; set; }
    public int MaxHolds { get; set; } = 3;
    public int HoldExpireDays { get; set; } = 3;

    public bool AllowLoan { get; set; } = true;
    public bool AllowRenew { get; set; } = true;
    public bool AllowHold { get; set; } = true;
    /// <summary>When false the copy may only be consulted inside the reading room.</summary>
    public bool AllowTakeHome { get; set; } = true;
    public bool RequireRenewalApproval { get; set; }

    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

/// <summary>A loan of one copy to one reader.</summary>
public class Loan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    /// <summary>Denormalised so loan history survives even if the record is re-catalogued.</summary>
    public Guid? BibId { get; set; }
    public string? BibTitle { get; set; }
    public string? Barcode { get; set; }

    public DateTimeOffset LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? ReturnDate { get; set; }
    public int RenewedCount { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public LoanType LoanType { get; set; } = LoanType.TakeHome;
    public LoanChannel Channel { get; set; } = LoanChannel.Desk;

    public Guid? LoanBy { get; set; }
    public string? LoanByName { get; set; }
    public Guid? ReturnBy { get; set; }
    public string? ReturnByName { get; set; }
    public Guid? PolicyId { get; set; }

    public decimal FineAmount { get; set; }
    public decimal FinePaid { get; set; }
    public string? Note { get; set; }

    /// <summary>Days past the due date, ignoring the grace period which is applied when fining.</summary>
    public int OverdueDays(DateOnly today)
    {
        var end = ReturnDate.HasValue ? DateOnly.FromDateTime(ReturnDate.Value.LocalDateTime) : today;
        var days = end.DayNumber - DueDate.DayNumber;
        return days > 0 ? days : 0;
    }
}

public class LoanRenewal : BaseEntity
{
    public Guid LoanId { get; set; }
    public Loan? Loan { get; set; }
    public DateTimeOffset RenewalDate { get; set; }
    public DateOnly OldDueDate { get; set; }
    public DateOnly NewDueDate { get; set; }
    public Guid? RequestedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public LoanChannel Channel { get; set; } = LoanChannel.Desk;
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Approved;
    public string? RejectReason { get; set; }
}

/// <summary>
/// Đặt giữ chỗ. A hold is placed either on a title (any free copy satisfies it) or on one specific
/// copy. Waiting readers are served in <see cref="QueuePosition"/> order.
/// </summary>
public class Hold : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    /// <summary>Null for a title-level hold; set once a copy has been assigned.</summary>
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public DateTimeOffset HoldDate { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public Guid? PickupWarehouseId { get; set; }
    public Warehouse? PickupWarehouse { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Waiting;
    public int QueuePosition { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
    public DateTimeOffset? FulfilledAt { get; set; }
    public LoanChannel Channel { get; set; } = LoanChannel.Opac;
    public string? CancelReason { get; set; }
}

public class Fine : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public Guid? LoanId { get; set; }
    public Loan? Loan { get; set; }
    public FineType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public Guid? PaidBy { get; set; }
    public string? PaidByName { get; set; }
    public bool Waived { get; set; }
    public string? WaiveReason { get; set; }
    public Guid? WaivedBy { get; set; }
    public string? Note { get; set; }

    public decimal Outstanding => Waived ? 0 : Amount - PaidAmount;
}

/// <summary>Tủ gửi đồ (VII.3).</summary>
public class Locker : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? LibraryId { get; set; }
    public string? Area { get; set; }
    public string? Size { get; set; }
    public LockerStatus Status { get; set; } = LockerStatus.Free;
    public int? MapRow { get; set; }
    public int? MapColumn { get; set; }
    public string? Note { get; set; }
}

public class LockerUsage : BaseEntity
{
    public Guid LockerId { get; set; }
    public Locker? Locker { get; set; }
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public DateTimeOffset CheckinAt { get; set; }
    public DateTimeOffset? CheckoutAt { get; set; }
    public string? KeyNumber { get; set; }
    public Guid? IssuedBy { get; set; }
    public string? Note { get; set; }
}

/// <summary>Gate check-in / check-out used by report VII.5.1.</summary>
public class LibraryVisit : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public Guid? LibraryId { get; set; }
    public DateTimeOffset CheckinAt { get; set; }
    public DateTimeOffset? CheckoutAt { get; set; }
    public string? Gate { get; set; }
    public string? Purpose { get; set; }
}

/// <summary>
/// Trạm mượn tự phục vụ (Phase 15, mục 3.2): một mã QR dán tại kho, bạn đọc quét trước khi tự mượn để
/// chứng minh mình đang đứng trong thư viện. Nội dung QR gồm mã trạm và chữ ký HMAC, nên chụp lại mã
/// đem về nhà vẫn cần đúng khoá của thư viện mới sinh ra được — và cán bộ tắt trạm là mã hết tác dụng.
/// </summary>
public class CheckoutStation : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
}
