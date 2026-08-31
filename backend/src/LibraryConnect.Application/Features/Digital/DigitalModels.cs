using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// Phân hệ V — Tài liệu số. Các kiểu dữ liệu dùng chung cho cả màn hình quản trị lẫn nhóm endpoint
// dành cho bạn đọc.
// ---------------------------------------------------------------------------------------------

/// <summary>Một nút trên cây bộ sưu tập (V.1).</summary>
public record DigitalCollectionDto(
    Guid Id,
    string Code,
    string Name,
    string? NameEn,
    Guid? ParentId,
    string? ParentName,
    string? Description,
    DigitalAccessLevel DefaultAccessLevel,
    int SortOrder,
    bool IsActive,
    int DocumentCount,
    IReadOnlyList<DigitalCollectionDto> Children);

/// <summary>Một dòng trên danh sách tài liệu số.</summary>
public record DigitalDocumentRowDto(
    Guid Id,
    string Title,
    string FileName,
    string MimeType,
    long FileSize,
    int? PageCount,
    Guid? CollectionId,
    string? CollectionName,
    Guid? BibId,
    string? BibTitle,
    DigitalAccessLevel AccessLevel,
    bool AllowDownload,
    bool AllowPrint,
    bool WatermarkEnabled,
    int PreviewPages,
    bool HasThumbnail,
    bool HasText,
    bool OcrProcessed,
    int ViewCount,
    int DownloadCount,
    string? UploadByName,
    DateTimeOffset UploadAt,
    // Đoạn văn bản chứa từ khóa, chỉ có khi tìm kiếm toàn văn.
    string? Snippet);

/// <summary>Chi tiết một tài liệu số kèm các bản dẫn xuất và quyền của người đang xem.</summary>
public record DigitalDocumentDetailDto(
    DigitalDocumentRowDto Document,
    string? Description,
    string? ChecksumSha256,
    IReadOnlyList<DigitalDocumentFileDto> Files,
    DigitalPermissionDto Permission);

public record DigitalDocumentFileDto(
    Guid Id, DigitalFileType Type, string Path, long Size, string? MimeType, int? PageNumber);

/// <summary>
/// Người đang gọi được làm gì với tài liệu này.
///
/// Máy chủ quyết định và trả xuống, chứ không để giao diện tự suy từ mức truy cập — mọi endpoint
/// đọc và tải đều kiểm lại đúng bộ quy tắc này.
/// </summary>
public record DigitalPermissionDto(
    bool CanRead,
    bool CanDownload,
    bool CanPrint,
    // Số trang được xem; null nghĩa là xem hết.
    int? ReadablePages,
    bool NeedsRequest,
    // Trạng thái yêu cầu bạn đọc đã gửi, nếu có.
    AccessRequestStatus? RequestStatus,
    DateTimeOffset? AccessExpireAt,
    string Reason);

/// <summary>Bộ lọc danh sách tài liệu số.</summary>
public class DigitalDocumentFilter
{
    public Guid? CollectionId { get; set; }
    /// <summary>Lấy cả tài liệu của các bộ sưu tập con.</summary>
    public bool IncludeDescendants { get; set; } = true;
    public Guid? BibId { get; set; }
    public DigitalAccessLevel? AccessLevel { get; set; }
    /// <summary>Nhóm định dạng: PDF, VIDEO, AUDIO, IMAGE, OFFICE, OTHER.</summary>
    public string? FormatGroup { get; set; }
    public bool? HasText { get; set; }
    /// <summary>Tìm trong nội dung tài liệu chứ không chỉ trong nhan đề.</summary>
    public bool FullText { get; set; }
    public DateOnly? UploadedFrom { get; set; }
    public DateOnly? UploadedTo { get; set; }
}

public class DigitalDocumentQueryRequest : PagedRequest
{
    public DigitalDocumentFilter Filter { get; set; } = new();
}

/// <summary>Một dòng trên danh sách yêu cầu đọc tài liệu hạn chế (V.2).</summary>
public record DigitalAccessRequestRowDto(
    Guid Id,
    Guid DocumentId,
    string DocumentTitle,
    Guid ReaderId,
    string ReaderName,
    string ReaderCardNumber,
    string? ReaderTypeName,
    string? FacultyName,
    DateTimeOffset RequestDate,
    string? Reason,
    AccessRequestStatus Status,
    string? ApprovedByName,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ExpireAt,
    string? RejectReason,
    int? MaxViews,
    int ViewCount,
    bool AllowDownload,
    // Số giờ từ lúc gửi tới lúc cán bộ xử lý — dùng cho báo cáo V.4.
    double? ProcessingHours);

/// <summary>Một dòng nhật ký truy cập tài liệu số (V.2).</summary>
public record DigitalAccessLogRowDto(
    Guid Id,
    Guid DocumentId,
    string DocumentTitle,
    Guid? ReaderId,
    string? ReaderName,
    string? ReaderCardNumber,
    string? UserName,
    DigitalAccessAction Action,
    string? Ip,
    string? Device,
    int? PageFrom,
    int? PageTo,
    int? DurationSeconds,
    DateTimeOffset OccurredAt);

/// <summary>Trạng thái một phiên tải tệp lớn theo mảnh.</summary>
public record DigitalUploadSessionDto(
    Guid Id,
    string FileName,
    long TotalSize,
    int ChunkSize,
    int TotalChunks,
    IReadOnlyList<int> ReceivedChunks,
    IReadOnlyList<int> MissingChunks,
    bool IsCompleted,
    Guid? DocumentId,
    DateTimeOffset ExpiresAt);
