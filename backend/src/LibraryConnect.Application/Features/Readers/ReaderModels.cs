using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// Phân hệ VI — Bạn đọc. Các kiểu dữ liệu dùng chung cho hồ sơ, thẻ, xuất nhập và báo cáo.
// ---------------------------------------------------------------------------------------------

/// <summary>Một dòng trên danh sách bạn đọc.</summary>
public class ReaderDto
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }

    public Guid ReaderTypeId { get; set; }
    public string? ReaderTypeName { get; set; }
    public Guid? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public Guid? MajorId { get; set; }
    public string? MajorName { get; set; }
    public string? ClassName { get; set; }
    public string? CourseYear { get; set; }

    public DateOnly CardIssueDate { get; set; }
    public DateOnly CardExpireDate { get; set; }
    public ReaderStatus Status { get; set; }
    public string? StatusReason { get; set; }

    public decimal DepositAmount { get; set; }
    public decimal DebtAmount { get; set; }
    public int CurrentLoanCount { get; set; }
    public int TotalLoanCount { get; set; }

    /// <summary>
    /// Thẻ đã quá hạn tính theo ngày hôm nay, kể cả khi trạng thái lưu trong hồ sơ vẫn là Hoạt động —
    /// hạn thẻ trôi qua hằng ngày nên không thể chờ ai đó bấm nút mới đổi trạng thái.
    /// </summary>
    public bool IsExpired { get; set; }
    /// <summary>Thẻ sắp hết hạn trong khoảng cảnh báo (mặc định 30 ngày).</summary>
    public bool IsExpiringSoon { get; set; }
    public bool CanBorrow { get; set; }
}

/// <summary>Hồ sơ đầy đủ của một bạn đọc, dùng cho màn hình chi tiết.</summary>
public class ReaderDetailDto : ReaderDto
{
    public string? IdCardNumber { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public bool HasPassword { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Lịch sử cấp thẻ: mỗi lần cấp lại giữ lại dòng của thẻ cũ.</summary>
    public List<ReaderCardDto> Cards { get; set; } = new();
}

public class ReaderCardDto
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpireDate { get; set; }
    public int PrintCount { get; set; }
    public bool IsCurrent { get; set; }
    public string? ReissueReason { get; set; }
}

/// <summary>Bộ lọc danh sách bạn đọc (VI.1).</summary>
public class ReaderListRequest : PagedRequest
{
    public Guid? ReaderTypeId { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? MajorId { get; set; }
    public string? ClassName { get; set; }
    public string? CourseYear { get; set; }
    public ReaderStatus? Status { get; set; }
    /// <summary>Chỉ những thẻ đã hết hạn tính theo ngày hôm nay.</summary>
    public bool? Expired { get; set; }
    /// <summary>Thẻ hết hạn trong vòng N ngày tới.</summary>
    public int? ExpiringInDays { get; set; }
    /// <summary>Chỉ bạn đọc còn nợ tiền hoặc còn sách chưa trả.</summary>
    public bool? HasDebt { get; set; }
    /// <summary>Chỉ bạn đọc đang giữ sách.</summary>
    public bool? Borrowing { get; set; }
    /// <summary>Chỉ bạn đọc chưa từng mượn quyển nào.</summary>
    public bool? NeverBorrowed { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
}

/// <summary>
/// Chọn bạn đọc cho một thao tác hàng loạt: theo danh sách tick chọn, hoặc theo đúng bộ lọc đang
/// xem trên màn hình.
///
/// Hai cách chọn tồn tại song song vì cán bộ làm hai kiểu khác nhau: gia hạn cho vài người thì tick,
/// còn gia hạn cho cả khóa K45 thì lọc rồi áp dụng — bắt tick tám trăm dòng là không dùng được.
/// </summary>
public class ReaderSelectionDto
{
    public List<Guid> ReaderIds { get; set; } = new();
    public ReaderListRequest? Filter { get; set; }
    public bool UseFilter { get; set; }
}

/// <summary>Kết quả của một thao tác hàng loạt, kèm danh sách trường hợp bị bỏ qua và lý do.</summary>
public class BulkResultDto
{
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Skipped { get; set; }
    public List<BulkSkipDto> Skips { get; set; } = new();
}

public record BulkSkipDto(Guid ReaderId, string CardNumber, string FullName, string Reason);
