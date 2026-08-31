using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Rdr;

/// <summary>
/// Bạn đọc. Readers authenticate against the OPAC and the mobile app with their card number, so the
/// password hash lives here rather than on sys.users.
/// </summary>
public class Reader : BaseEntity
{
    public string CardNumber { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    /// <summary>Nam | Nữ | Khác — kept as text so the customer can extend the list.</summary>
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? IdCardNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PhotoUrl { get; set; }

    public Guid ReaderTypeId { get; set; }
    public ReaderType? ReaderType { get; set; }
    public Guid? FacultyId { get; set; }
    public Faculty? Faculty { get; set; }
    public Guid? MajorId { get; set; }
    public Major? Major { get; set; }
    public string? ClassName { get; set; }
    public string? CourseYear { get; set; }

    public DateOnly CardIssueDate { get; set; }
    public DateOnly CardExpireDate { get; set; }
    public ReaderStatus Status { get; set; } = ReaderStatus.Active;
    public string? StatusReason { get; set; }

    public decimal DepositAmount { get; set; }
    public decimal DebtAmount { get; set; }
    public string? Note { get; set; }

    public string? PasswordHash { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }

    public int CurrentLoanCount { get; set; }
    public int TotalLoanCount { get; set; }

    public ICollection<ReaderCard> Cards { get; set; } = new List<ReaderCard>();
    public ICollection<ReaderViolation> Violations { get; set; } = new List<ReaderViolation>();

    /// <summary>A reader may borrow only with an active, unexpired and unlocked card.</summary>
    public bool CanBorrow(DateOnly today) =>
        Status == ReaderStatus.Active && CardExpireDate >= today && LockedUntil is null;
}

/// <summary>Card issue history. Re-issuing a card keeps the previous rows for the audit trail.</summary>
public class ReaderCard : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpireDate { get; set; }
    public int PrintCount { get; set; }
    public Guid? TemplateId { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string? ReissueReason { get; set; }
}

/// <summary>Card layout designer output, front and back, CR80 by default (VI.2).</summary>
public class ReaderCardTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 85.6;
    public double HeightMm { get; set; } = 54;
    public string FrontLayout { get; set; } = "{}";
    public string BackLayout { get; set; } = "{}";
    public string? BackgroundImageUrl { get; set; }
    /// <summary>In cả mặt sau; tắt thì chỉ in mặt trước.</summary>
    public bool PrintBack { get; set; }
    public int CardsPerPage { get; set; } = 10;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Result of one reader Excel import run (VI.4).</summary>
public class ReaderImportBatch : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    /// <summary>jsonb array of {row, column, message} so the UI can render an editable error grid.</summary>
    public string? Errors { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public DateTimeOffset? FinishedAt { get; set; }
}

public class ReaderViolation : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public Guid? ViolationTypeId { get; set; }
    public ViolationType? ViolationType { get; set; }
    public string? Description { get; set; }
    public decimal FineAmount { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}

/// <summary>Yêu cầu gia hạn thẻ gửi từ OPAC hoặc app (endpoint /api/reader/card/renew-request).</summary>
public class CardRenewalRequest : BaseEntity
{
    public Guid ReaderId { get; set; }
    public Reader? Reader { get; set; }
    public DateTimeOffset RequestDate { get; set; }
    public string? Reason { get; set; }
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public Guid? ProcessedBy { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateOnly? NewExpireDate { get; set; }
}
