using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Acq;

/// <summary>Yêu cầu đặt mua (III.1) — for monographs or for serial subscriptions.</summary>
public class PurchaseRequest : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public PurchaseRequestType Type { get; set; }
    public Guid? RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateOnly RequestDate { get; set; }
    public string? Reason { get; set; }
    public Guid? FundingSourceId { get; set; }
    public FundingSource? FundingSource { get; set; }
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;
    public DateTimeOffset? SubmittedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    /// <summary>Current step of a configurable multi-level approval flow.</summary>
    public int ApprovalLevel { get; set; }

    public ICollection<PurchaseRequestItem> Items { get; set; } = new List<PurchaseRequestItem>();
}

public class PurchaseRequestItem : BaseEntity
{
    public Guid RequestId { get; set; }
    public PurchaseRequest? Request { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? PublisherName { get; set; }
    public int? PublishYear { get; set; }
    public string? Isbn { get; set; }
    public string? Issn { get; set; }
    public int Quantity { get; set; } = 1;
    public int ApprovedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal EstimatedAmount { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    /// <summary>Set when the duplicate check matched an existing title.</summary>
    public Guid? BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public bool IsDuplicate { get; set; }
    public string? Note { get; set; }
    // Serial-specific fields
    public SerialFrequency? Frequency { get; set; }
    public int? IssuesPerYear { get; set; }
    public DateOnly? SubscriptionFrom { get; set; }
    public DateOnly? SubscriptionTo { get; set; }
}

/// <summary>Đơn đặt hàng gửi nhà cung cấp.</summary>
public class PurchaseOrder : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public Guid? FundingSourceId { get; set; }
    public FundingSource? FundingSource { get; set; }
    public string? ContractNo { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.New;
    public string? Note { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public PurchaseOrder? Order { get; set; }
    public Guid? RequestItemId { get; set; }
    public PurchaseRequestItem? RequestItem { get; set; }
    public Guid? BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReceivedQuantity { get; set; }
    public string? Note { get; set; }
}

/// <summary>Biên bản bàn giao between supplier and library.</summary>
public class HandoverRecord : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public PurchaseOrder? Order { get; set; }
    public DateOnly HandoverDate { get; set; }
    public string PartyA { get; set; } = string.Empty;
    public string PartyB { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Scanned signed copy stored in MinIO.</summary>
    public string? FileUrl { get; set; }
    public string? Note { get; set; }
}
