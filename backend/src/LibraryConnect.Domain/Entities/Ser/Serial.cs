using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Ser;

/// <summary>
/// A subscribed newspaper or journal title (Phân hệ IV). The bibliographic description lives in
/// <see cref="BibRecord"/>; this row carries the subscription and the publication pattern.
/// </summary>
public class Serial : BaseEntity
{
    public Guid BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Issn { get; set; }
    public Guid? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    public Guid? LanguageId { get; set; }
    public Language? Language { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public SerialFrequency Frequency { get; set; } = SerialFrequency.Monthly;
    /// <summary>
    /// JSON pattern driving issue prediction: issues per year, publication weekday or day of month,
    /// numbering scheme (continuous / restart each year / volume+issue), starting volume and issue,
    /// and the skipped periods (e.g. no July/August issue).
    /// </summary>
    public string FrequencyConfig { get; set; } = "{}";

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? ShelfId { get; set; }
    public string? CallNumber { get; set; }

    public DateOnly? SubscriptionStart { get; set; }
    public DateOnly? SubscriptionEnd { get; set; }
    public decimal? PricePerIssue { get; set; }
    public int CopiesPerIssue { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }

    public ICollection<SerialIssue> Issues { get; set; } = new List<SerialIssue>();
}

/// <summary>One predicted or received issue.</summary>
public class SerialIssue : BaseEntity
{
    public Guid SerialId { get; set; }
    public Serial? Serial { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public string? Volume { get; set; }
    public int Year { get; set; }
    /// <summary>Human readable label such as "Tập 12, Số 3 (2025)".</summary>
    public string? Caption { get; set; }
    /// <summary>Predicted publication date.</summary>
    public DateOnly ExpectedDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public Guid? ReceivedBy { get; set; }
    public string? ReceivedByName { get; set; }
    public int Quantity { get; set; }
    public SerialIssueStatus Status { get; set; } = SerialIssueStatus.Expected;
    public string? Barcode { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BindingId { get; set; }
    /// <summary>Tình trạng vật lý lúc nhận số báo (IV.4): nguyên vẹn, rách bìa, thiếu trang…</summary>
    public string? Condition { get; set; }
    public string? Note { get; set; }

    public ICollection<SerialIssueArticle> Articles { get; set; } = new List<SerialIssueArticle>();
}

/// <summary>
/// Bài trích — an article inside an issue (IV.2). Each one can be promoted to its own MARC record
/// linked back to the host item through field 773, so it becomes searchable in the OPAC.
/// </summary>
public class SerialIssueArticle : BaseEntity
{
    public Guid IssueId { get; set; }
    public SerialIssue? Issue { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Authors { get; set; }
    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }
    /// <summary>Set when a separate analytic MARC record has been generated for this article.</summary>
    public Guid? BibId { get; set; }
    public BibRecord? Bib { get; set; }
}

/// <summary>Đóng tập — a run of issues bound into one volume, which becomes a new copy.</summary>
public class SerialBinding : BaseEntity
{
    public Guid SerialId { get; set; }
    public Serial? Serial { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? FromIssue { get; set; }
    public string? ToIssue { get; set; }
    public int Year { get; set; }
    public DateOnly BindingDate { get; set; }
    /// <summary>The copy created for the bound volume.</summary>
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public int IssueCount { get; set; }
    public string? Note { get; set; }
}

/// <summary>Khiếu nại nhà cung cấp about a missing issue (IV.3).</summary>
public class SerialClaim : BaseEntity
{
    public Guid IssueId { get; set; }
    public SerialIssue? Issue { get; set; }
    public string ClaimNo { get; set; } = string.Empty;
    public DateOnly ClaimDate { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? Content { get; set; }
    public string? Response { get; set; }
    public DateOnly? ResponseDate { get; set; }
    public SerialClaimStatus Status { get; set; } = SerialClaimStatus.Open;
}
