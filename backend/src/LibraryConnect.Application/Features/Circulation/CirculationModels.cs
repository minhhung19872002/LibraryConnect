using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// Phân hệ VII — Lưu thông. Kiểu dữ liệu dùng chung cho quầy, đặt giữ, tiền phạt và báo cáo.
// ---------------------------------------------------------------------------------------------

/// <summary>Một cảnh báo hiện ở quầy khi quét thẻ hoặc quét mã vạch.</summary>
/// <param name="Code">Mã cảnh báo để giao diện tô màu và phát âm thanh.</param>
/// <param name="Message">Câu tiếng Việt hiện cho cán bộ.</param>
/// <param name="Blocking">True là chặn hẳn giao dịch, false chỉ là nhắc.</param>
public record CirculationWarningDto(string Code, string Message, bool Blocking);

public static class CirculationWarnings
{
    public const string CardExpired = "CARD_EXPIRED";
    public const string CardExpiringSoon = "CARD_EXPIRING";
    public const string ReaderLocked = "READER_LOCKED";
    public const string ReaderGraduated = "READER_GRADUATED";
    public const string Debt = "DEBT";
    public const string OverdueLoans = "OVERDUE";
    public const string LimitReached = "LIMIT";
    public const string HoldReady = "HOLD_READY";
    public const string ItemNotFound = "ITEM_NOT_FOUND";
    public const string ItemNotAvailable = "ITEM_NOT_AVAILABLE";
    public const string ItemLocked = "ITEM_LOCKED";
    public const string ItemOnLoan = "ITEM_ON_LOAN";
    public const string ItemHeldForOther = "ITEM_HELD_FOR_OTHER";
    public const string PolicyForbidsLoan = "POLICY_NO_LOAN";
    public const string PolicyForbidsTakeHome = "POLICY_NO_TAKE_HOME";
    public const string AlreadyInList = "ALREADY_IN_LIST";
}

/// <summary>Thông tin bạn đọc hiện ở quầy ngay sau khi quét thẻ (VII.2).</summary>
public class DeskReaderDto
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ReaderTypeName { get; set; }
    public Guid ReaderTypeId { get; set; }
    public string? FacultyName { get; set; }
    public string? ClassName { get; set; }
    public bool HasPhoto { get; set; }
    public ReaderStatus Status { get; set; }
    public DateOnly CardExpireDate { get; set; }
    public bool CanBorrow { get; set; }

    public int CurrentLoanCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OutstandingFines { get; set; }
    public int MaxItems { get; set; }
    /// <summary>Số tài liệu còn được mượn thêm theo chính sách; âm nghĩa là đã vượt.</summary>
    public int RemainingQuota { get; set; }

    public List<CirculationWarningDto> Warnings { get; set; } = new();
    public List<LoanRowDto> CurrentLoans { get; set; } = new();
    public List<HoldRowDto> ReadyHolds { get; set; } = new();
}

/// <summary>Một lượt mượn hiện trên danh sách.</summary>
public class LoanRowDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid ReaderId { get; set; }
    public string ReaderCardNumber { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public string? ClassName { get; set; }
    public Guid ItemId { get; set; }
    public string? Barcode { get; set; }
    public string? Title { get; set; }
    public string? CallNumber { get; set; }
    public string? WarehouseName { get; set; }
    public DateTimeOffset LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? ReturnDate { get; set; }
    public int RenewedCount { get; set; }
    public int MaxRenewals { get; set; }
    public LoanStatus Status { get; set; }
    public LoanType LoanType { get; set; }
    public LoanChannel Channel { get; set; }
    public string? LoanByName { get; set; }
    public string? ReturnByName { get; set; }
    public decimal FineAmount { get; set; }
    public decimal FineOutstanding { get; set; }
    /// <summary>Số ngày quá hạn tính tới hôm nay (hoặc tới ngày trả).</summary>
    public int OverdueDays { get; set; }
    /// <summary>Tiền phạt dự kiến nếu trả hôm nay, dùng cho báo cáo quá hạn.</summary>
    public decimal EstimatedFine { get; set; }
    public string? Note { get; set; }
}

/// <summary>Kết quả quét một mã vạch ở màn hình ghi mượn.</summary>
public class ScanForLoanDto
{
    public bool Allowed { get; set; }
    public Guid? ItemId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string? RegisterNumber { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? CallNumber { get; set; }
    public string? WarehouseName { get; set; }
    public string? DocumentTypeName { get; set; }
    public ItemStatus? ItemStatus { get; set; }
    /// <summary>Hạn trả dự kiến nếu ghi mượn ngay bây giờ — do máy chủ tính, không phải màn hình.</summary>
    public DateOnly? DueDate { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public bool AllowTakeHome { get; set; } = true;
    public List<CirculationWarningDto> Warnings { get; set; } = new();
}

/// <summary>Kết quả ghi mượn một lô mã vạch.</summary>
public class CheckoutResultDto
{
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public List<LoanRowDto> Loans { get; set; } = new();
    public List<CirculationFailureDto> Failures { get; set; } = new();
    /// <summary>Mã phiếu mượn để in ngay sau khi hoàn tất.</summary>
    public string? SlipCode { get; set; }
}

public record CirculationFailureDto(string Barcode, string Message);

/// <summary>Kết quả ghi trả một lô mã vạch.</summary>
public class ReturnResultDto
{
    public List<ReturnedItemDto> Items { get; set; } = new();
    public List<CirculationFailureDto> Failures { get; set; } = new();
    public decimal TotalFine { get; set; }
    public string? SlipCode { get; set; }
}

public class ReturnedItemDto
{
    public Guid LoanId { get; set; }
    public string LoanCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string ReaderCardNumber { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int OverdueDays { get; set; }
    public decimal Fine { get; set; }
    public string? FineCode { get; set; }
    /// <summary>Bản này có người đang đợi: giữ lại quầy thay vì trả lên giá.</summary>
    public bool HoldWaiting { get; set; }
    public string? HoldForReaderName { get; set; }
    public string? HoldPickupWarehouse { get; set; }
    public List<CirculationWarningDto> Warnings { get; set; } = new();
}

/// <summary>Một đặt giữ chỗ.</summary>
public class HoldRowDto
{
    public Guid Id { get; set; }
    public Guid ReaderId { get; set; }
    public string ReaderCardNumber { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public Guid BibId { get; set; }
    public string? Title { get; set; }
    public Guid? ItemId { get; set; }
    public string? Barcode { get; set; }
    public DateTimeOffset HoldDate { get; set; }
    public DateTimeOffset? ExpireDate { get; set; }
    public Guid? PickupWarehouseId { get; set; }
    public string? PickupWarehouseName { get; set; }
    public HoldStatus Status { get; set; }
    public int QueuePosition { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
    public LoanChannel Channel { get; set; }
    public string? CancelReason { get; set; }
    /// <summary>Số bản đang rảnh của biểu ghi, để cán bộ biết có lấy được ngay không.</summary>
    public int AvailableCopies { get; set; }
}

/// <summary>Một khoản phạt.</summary>
public class FineRowDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid ReaderId { get; set; }
    public string ReaderCardNumber { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public Guid? LoanId { get; set; }
    public string? LoanCode { get; set; }
    public string? Title { get; set; }
    public string? Barcode { get; set; }
    public FineType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
    public bool Waived { get; set; }
    public string? WaiveReason { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? PaidByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>Một tủ gửi đồ trên sơ đồ.</summary>
public class LockerRowDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? LibraryId { get; set; }
    public string? LibraryName { get; set; }
    public string? Area { get; set; }
    public string? Size { get; set; }
    public LockerStatus Status { get; set; }
    public int? MapRow { get; set; }
    public int? MapColumn { get; set; }
    public string? Note { get; set; }

    // Lượt sử dụng đang mở, nếu tủ đang có người dùng.
    public Guid? UsageId { get; set; }
    public Guid? ReaderId { get; set; }
    public string? ReaderName { get; set; }
    public string? ReaderCardNumber { get; set; }
    public DateTimeOffset? CheckinAt { get; set; }
    public string? KeyNumber { get; set; }
    /// <summary>Số phút đã giữ tủ, để cảnh báo tủ quá giờ.</summary>
    public int? MinutesInUse { get; set; }
    public bool Overdue { get; set; }
}

public class LockerUsageRowDto
{
    public Guid Id { get; set; }
    public Guid LockerId { get; set; }
    public string LockerCode { get; set; } = string.Empty;
    public string? Area { get; set; }
    public Guid ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string ReaderCardNumber { get; set; } = string.Empty;
    public DateTimeOffset CheckinAt { get; set; }
    public DateTimeOffset? CheckoutAt { get; set; }
    public int? Minutes { get; set; }
    public string? KeyNumber { get; set; }
    public string? Note { get; set; }
}

/// <summary>Một lượt ra vào thư viện.</summary>
public class VisitRowDto
{
    public Guid Id { get; set; }
    public Guid ReaderId { get; set; }
    public string ReaderCardNumber { get; set; } = string.Empty;
    public string ReaderName { get; set; } = string.Empty;
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public Guid? LibraryId { get; set; }
    public string? LibraryName { get; set; }
    public DateTimeOffset CheckinAt { get; set; }
    public DateTimeOffset? CheckoutAt { get; set; }
    public int? Minutes { get; set; }
    public string? Gate { get; set; }
    public string? Purpose { get; set; }
}

/// <summary>Bộ lọc chung của danh sách mượn trả.</summary>
public class LoanListRequest : PagedRequest
{
    public Guid? ReaderId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ReaderTypeId { get; set; }
    public Guid? FacultyId { get; set; }
    public LoanStatus? Status { get; set; }
    public LoanType? LoanType { get; set; }
    public LoanChannel? Channel { get; set; }
    /// <summary>Chỉ các lượt chưa trả.</summary>
    public bool? ActiveOnly { get; set; }
    /// <summary>Chỉ các lượt đang quá hạn.</summary>
    public bool? OverdueOnly { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class HoldListRequest : PagedRequest
{
    public Guid? ReaderId { get; set; }
    public Guid? BibId { get; set; }
    public HoldStatus? Status { get; set; }
    public Guid? PickupWarehouseId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public class FineListRequest : PagedRequest
{
    public Guid? ReaderId { get; set; }
    public FineType? Type { get; set; }
    public bool? OutstandingOnly { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class VisitListRequest : PagedRequest
{
    public Guid? ReaderId { get; set; }
    public Guid? LibraryId { get; set; }
    public bool? InsideOnly { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
