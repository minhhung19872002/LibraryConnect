using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// Phân hệ IX — Tra cứu. Các kiểu dữ liệu dùng chung cho trang tra cứu công khai và cho ứng dụng
// di động đợt sau (mục XI.4): cùng một hợp đồng, cùng một bộ endpoint.
// ---------------------------------------------------------------------------------------------

/// <summary>Phạm vi của ô tìm kiếm cơ bản.</summary>
public enum OpacSearchScope
{
    /// <summary>Tất cả</summary>
    All,
    Title,
    Author,
    Subject,
    Keyword,
    Publisher,
    Isbn,
    /// <summary>Ký hiệu xếp giá / số đăng ký cá biệt.</summary>
    CallNumber
}

/// <summary>Cách sắp xếp kết quả.</summary>
public enum OpacSortOrder
{
    /// <summary>Liên quan nhất</summary>
    Relevance,
    /// <summary>Mới nhất</summary>
    Newest,
    Title,
    Author,
    /// <summary>Được mượn nhiều</summary>
    Popular
}

/// <summary>Cách nối một mệnh đề trong tìm kiếm nâng cao với phần trước nó.</summary>
public enum OpacConnector { And, Or, Not }

public static class OpacScopeLabels
{
    public static string Describe(OpacSearchScope scope) => scope switch
    {
        OpacSearchScope.Title => "Nhan đề",
        OpacSearchScope.Author => "Tác giả",
        OpacSearchScope.Subject => "Chủ đề",
        OpacSearchScope.Keyword => "Từ khóa",
        OpacSearchScope.Publisher => "Nhà xuất bản",
        OpacSearchScope.Isbn => "ISBN / ISSN",
        OpacSearchScope.CallNumber => "Ký hiệu xếp giá",
        _ => "Tất cả"
    };
}

/// <summary>Bộ lọc dùng chung cho tìm kiếm cơ bản, tìm kiếm nâng cao và bộ đếm facet.</summary>
public class OpacFilter
{
    public int? PublishYearFrom { get; set; }
    public int? PublishYearTo { get; set; }
    public Guid? LanguageId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? CollectionId { get; set; }
    public Guid? PublisherId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CourseId { get; set; }

    /// <summary>Ký hiệu phân loại: lọc theo tiền tố, ví dụ "005" lấy cả 005.1, 005.74.</summary>
    public string? Ddc { get; set; }

    /// <summary>Chỉ tài liệu có bản số đính kèm.</summary>
    public bool? HasDigital { get; set; }

    /// <summary>Chỉ tài liệu còn bản rảnh trong kho.</summary>
    public bool? AvailableOnly { get; set; }
}

public class OpacSearchRequest : PagedRequest
{
    public OpacSearchScope Scope { get; set; } = OpacSearchScope.All;
    public OpacSortOrder Sort { get; set; } = OpacSortOrder.Relevance;
    public OpacFilter Filter { get; set; } = new();
}

/// <summary>Một mệnh đề của tìm kiếm nâng cao.</summary>
public class OpacSearchClause
{
    public OpacConnector Connector { get; set; } = OpacConnector.And;
    public OpacSearchScope Field { get; set; } = OpacSearchScope.All;
    public string Term { get; set; } = string.Empty;
}

public class OpacAdvancedSearchRequest : PagedRequest
{
    public List<OpacSearchClause> Clauses { get; set; } = new();
    public OpacSortOrder Sort { get; set; } = OpacSortOrder.Relevance;
    public OpacFilter Filter { get; set; } = new();
}

/// <summary>Một dòng kết quả tra cứu.</summary>
public record OpacResultDto(
    Guid Id,
    string ControlNumber,
    string Title,
    string? Subtitle,
    string? AuthorMain,
    string? PublisherName,
    int? PublishYear,
    string? Isbn,
    string? Ddc,
    string? DocumentTypeName,
    string? LanguageName,
    string? CoverImageUrl,
    string? Abstract,
    int ItemCount,
    int AvailableItemCount,
    int DigitalDocumentCount,
    int LoanCount);

/// <summary>Một giá trị trong bộ lọc bên trái kèm số lượng tài liệu.</summary>
public record OpacFacetValueDto(string? Id, string Label, int Count);

public record OpacFacetGroupDto(string Code, string Name, IReadOnlyList<OpacFacetValueDto> Values);

public record OpacSuggestionDto(string Text, string Type, int Count);

/// <summary>Một bản in cụ thể trong màn hình chi tiết tài liệu.</summary>
public record OpacItemDto(
    Guid Id,
    string Barcode,
    string RegisterNumber,
    string? CallNumber,
    string LibraryName,
    string WarehouseName,
    string? ShelfName,
    string StatusLabel,
    bool IsAvailable,
    DateOnly? DueDate);

public record OpacDigitalDocumentDto(
    Guid Id,
    string Title,
    string FileName,
    string? MimeType,
    long FileSize,
    int? PageCount,
    string AccessLevelLabel,
    bool RequiresRequest,
    bool AllowDownload);

public record OpacReviewDto(
    Guid Id,
    string ReaderName,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

/// <summary>Chi tiết một tài liệu trên trang tra cứu.</summary>
public record OpacBibDetailDto(
    Guid Id,
    string ControlNumber,
    string Title,
    string? Subtitle,
    string? StatementOfResponsibility,
    string? AuthorMain,
    IReadOnlyList<OpacLinkedTermDto> Authors,
    IReadOnlyList<OpacLinkedTermDto> Subjects,
    IReadOnlyList<OpacLinkedTermDto> Keywords,
    IReadOnlyList<OpacLinkedTermDto> Classifications,
    string? PublisherName,
    string? PublishPlace,
    int? PublishYear,
    string? Edition,
    string? Pages,
    string? Dimensions,
    string? Isbn,
    string? Issn,
    string? Ddc,
    string? SeriesName,
    string? LanguageName,
    string? DocumentTypeName,
    string? Abstract,
    string? CoverImageUrl,
    string Isbd,
    string MarcJson,
    int ItemCount,
    int AvailableItemCount,
    IReadOnlyList<OpacItemDto> Items,
    IReadOnlyList<OpacDigitalDocumentDto> DigitalDocuments,
    IReadOnlyList<OpacExternalLinkDto> ExternalLinks,
    IReadOnlyList<OpacReviewDto> Reviews,
    double? AverageRating,
    IReadOnlyList<OpacResultDto> Related);

/// <summary>
/// Bản toàn văn nằm ở máy chủ khác, lấy từ trường MARC 856.
///
/// Biểu ghi thu hoạch về từ kho số của thư viện bạn hầu hết chỉ có địa chỉ tệp chứ không có tệp:
/// mình được phép mô tả tài liệu, không được phép giữ bản sao. Không hiện địa chỉ ấy ra thì bạn đọc
/// thấy thẻ "Tài liệu số (0)" trong khi bản toàn văn nằm ngay sau một cú bấm — vừa mất công tra
/// cứu, vừa làm người xem tưởng kho mình rỗng.
/// </summary>
public record OpacExternalLinkDto(string Url, string? Label, string? Note, string? MimeType);

/// <summary>Một tên tác giả / chủ đề bấm được để tra tiếp.</summary>
public record OpacLinkedTermDto(Guid? Id, string Name, string? Note);

/// <summary>Một mục trong màn hình duyệt theo chủ đề / tác giả / phân loại.</summary>
public record OpacBrowseEntryDto(
    Guid? Id,
    string Code,
    string Name,
    int BibCount,
    Guid? ParentId,
    bool HasChildren);

/// <summary>Nội dung trang chủ của trang tra cứu.</summary>
public record OpacHomeDto(
    IReadOnlyList<OpacResultDto> NewBooks,
    IReadOnlyList<OpacResultDto> PopularBooks,
    IReadOnlyList<OpacHomeNewsDto> News,
    IReadOnlyList<OpacHomeBannerDto> Banners,
    IReadOnlyList<OpacHomeLinkDto> Links,
    OpacStatisticsDto Statistics);

public record OpacHomeNewsDto(
    Guid Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailUrl,
    string? CategoryName,
    bool IsFeatured,
    DateTimeOffset? PublishedAt);

public record OpacHomeBannerDto(Guid Id, string Title, string ImageUrl, string? Link);

public record OpacHomeLinkDto(Guid Id, string Name, string Url, string? LogoUrl, string? GroupName);

/// <summary>Vài con số giới thiệu quy mô kho tư liệu, hiện ở trang chủ.</summary>
public record OpacStatisticsDto(
    int BibCount,
    int ItemCount,
    int DigitalCount,
    int ReaderCount);
