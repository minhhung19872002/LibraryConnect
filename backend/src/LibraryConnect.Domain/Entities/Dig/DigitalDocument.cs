using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Domain.Entities.Dig;

/// <summary>Cây bộ sưu tập tài liệu số (V.1).</summary>
public class DigitalCollection : HierarchicalCatalogEntity
{
    /// <summary>Access level inherited by documents created inside this collection.</summary>
    public DigitalAccessLevel DefaultAccessLevel { get; set; } = DigitalAccessLevel.Internal;
    public string? ImageUrl { get; set; }
}

/// <summary>
/// A digital object attached to a bibliographic record. The binary lives in MinIO; this row keeps
/// the metadata, the access policy and the usage counters.
/// </summary>
public class DigitalDocument : BaseEntity
{
    public Guid? BibId { get; set; }
    public BibRecord? Bib { get; set; }
    public Guid? CollectionId { get; set; }
    public DigitalCollection? Collection { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    /// <summary>Object key inside the MinIO bucket, never a path under the web root.</summary>
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public int? PageCount { get; set; }
    public string? ChecksumSha256 { get; set; }

    public DigitalAccessLevel AccessLevel { get; set; } = DigitalAccessLevel.Internal;
    public bool AllowDownload { get; set; }
    public bool AllowPrint { get; set; }
    public bool WatermarkEnabled { get; set; } = true;
    /// <summary>How many pages an unauthorised reader may preview.</summary>
    public int PreviewPages { get; set; } = 10;

    /// <summary>Plain text extracted from the file (or produced by OCR) for full-text search.</summary>
    public string? ExtractedText { get; set; }
    public bool OcrProcessed { get; set; }
    public DateTimeOffset? OcrProcessedAt { get; set; }

    public Guid? UploadBy { get; set; }
    public string? UploadByName { get; set; }
    public DateTimeOffset UploadAt { get; set; }
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }

    public ICollection<DigitalDocumentFile> Files { get; set; } = new List<DigitalDocumentFile>();
}

/// <summary>Derived renditions of a document: preview, thumbnail, OCR text layer.</summary>
public class DigitalDocumentFile : BaseEntity
{
    public Guid DocumentId { get; set; }
    public DigitalDocument? Document { get; set; }
    public DigitalFileType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? MimeType { get; set; }
    public int? PageNumber { get; set; }
}

/// <summary>Yêu cầu đọc tài liệu hạn chế (V.2).</summary>
public class DigitalAccessRequest : BaseEntity
{
    public Guid DocumentId { get; set; }
    public DigitalDocument? Document { get; set; }
    public Guid ReaderId { get; set; }
    public DateTimeOffset RequestDate { get; set; }
    public string? Reason { get; set; }
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }
    public string? RejectReason { get; set; }
    public int? MaxViews { get; set; }
    public int ViewCount { get; set; }
    public bool AllowDownload { get; set; }

    /// <summary>An approval is usable while it is approved, not expired and under its view quota.</summary>
    public bool IsUsable(DateTimeOffset now) =>
        Status == AccessRequestStatus.Approved
        && (ExpireAt is null || ExpireAt > now)
        && (MaxViews is null || ViewCount < MaxViews);
}

/// <summary>Detailed usage log required by V.2: who read what, which pages, from where, how long.</summary>
public class DigitalAccessLog : BaseEntity
{
    public Guid DocumentId { get; set; }
    public DigitalDocument? Document { get; set; }
    public Guid? ReaderId { get; set; }
    public Guid? UserId { get; set; }
    public DigitalAccessAction Action { get; set; }
    public string? Ip { get; set; }
    public string? Device { get; set; }
    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
