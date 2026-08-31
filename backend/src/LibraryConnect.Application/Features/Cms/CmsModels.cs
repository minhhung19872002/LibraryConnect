using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Features.Cms;

// ---------------------------------------------------------------------------------------------
// Phân hệ VIII — Quản trị nội dung. Các kiểu dữ liệu dùng chung cho màn hình quản trị và cho
// nhóm endpoint công khai mà trang tra cứu đọc.
// ---------------------------------------------------------------------------------------------

/// <summary>Trang tĩnh trong danh sách quản trị.</summary>
public record CmsPageRowDto(
    Guid Id,
    string Slug,
    string Title,
    string? MetaDescription,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    int SortOrder,
    Guid? ParentId,
    DateTimeOffset? UpdatedAt);

/// <summary>Trang tĩnh kèm nội dung đầy đủ.</summary>
public record CmsPageDto(
    Guid Id,
    string Slug,
    string Title,
    string? Content,
    string? MetaDescription,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    int SortOrder,
    Guid? ParentId);

public class CmsPageListRequest : PagedRequest
{
    public bool? IsPublished { get; set; }
}

public record CmsNewsRowDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailUrl,
    Guid? CategoryId,
    string? CategoryName,
    string? Tags,
    string? Author,
    bool IsFeatured,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    DateTimeOffset? UpdatedAt);

public record CmsNewsDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string? Content,
    string? ThumbnailUrl,
    Guid? CategoryId,
    string? CategoryName,
    string? Tags,
    string? Author,
    bool IsFeatured,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount);

public class CmsNewsListRequest : PagedRequest
{
    public Guid? CategoryId { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsFeatured { get; set; }
}

public record CmsBannerDto(
    Guid Id,
    string Title,
    string ImageUrl,
    string? Link,
    string Position,
    int SortOrder,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive);

public record CmsMenuDto(
    Guid Id,
    string Name,
    string? Url,
    Guid? ParentId,
    int SortOrder,
    string? Target,
    string? Icon,
    bool IsActive,
    IReadOnlyList<CmsMenuDto> Children);

public record CmsExternalLinkDto(
    Guid Id,
    string Name,
    string Url,
    string? LogoUrl,
    string? Description,
    string? GroupName,
    int SortOrder,
    bool IsActive);

public record CmsGalleryDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverUrl,
    DateOnly? EventDate,
    bool IsPublished,
    IReadOnlyList<CmsGalleryImageDto> Images);

public record CmsGalleryImageDto(Guid Id, string ImageUrl, string? Caption, int SortOrder);

/// <summary>
/// Một ô cấu hình trên màn hình "Thông tin trang thư viện".
///
/// Hai kho cấu hình cùng hiện trên một màn hình: tên/địa chỉ/logo nằm ở tham số hệ thống vì cả
/// phần mềm dùng chung, còn khẩu hiệu, giờ mở cửa, mạng xã hội chỉ trang tra cứu dùng nên nằm ở
/// bảng riêng. Trường <see cref="Store"/> cho biết giá trị sẽ được ghi về đâu.
/// </summary>
public record CmsSettingItemDto(
    string Key,
    string? Value,
    string Name,
    string? Description,
    string DataType,
    string GroupCode,
    string GroupName,
    string Store,
    int SortOrder);

public record CmsSettingGroupDto(string Code, string Name, IReadOnlyList<CmsSettingItemDto> Items);

public record CmsReviewRowDto(
    Guid Id,
    Guid BibId,
    string BibTitle,
    Guid ReaderId,
    string ReaderName,
    string? ReaderCardNumber,
    int Rating,
    string? Comment,
    bool IsApproved,
    DateTimeOffset CreatedAt);

public class CmsReviewListRequest : PagedRequest
{
    public bool? IsApproved { get; set; }
}

/// <summary>Ảnh tải lên cho tin tức, banner, album — trả về đường dẫn để nhúng vào nội dung.</summary>
public record CmsMediaDto(string ObjectName, string Url, string ContentType, long SizeBytes);
