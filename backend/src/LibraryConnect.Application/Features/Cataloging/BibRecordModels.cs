using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Một dòng trong danh sách biểu ghi.</summary>
public class BibListItemDto
{
    public Guid Id { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? AuthorMain { get; set; }
    public string? PublisherName { get; set; }
    public int? PublishYear { get; set; }
    public string? Isbn { get; set; }
    public string? Ddc { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? LanguageName { get; set; }
    public RecordStatus Status { get; set; }
    public BibSource Source { get; set; }
    /// <summary>Số đăng ký cá biệt đã tạo cho biểu ghi này.</summary>
    public int ItemCount { get; set; }
    public int AvailableItemCount { get; set; }
    public int DigitalDocumentCount { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>Ảnh bìa lấy từ đâu: Manual, Marc856, GoogleBooks, OpenLibrary.</summary>
    public string? CoverImageSource { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>Chi tiết một biểu ghi, đủ cho cả bốn tab của màn hình xem chi tiết (II.3).</summary>
public class BibDetailDto : BibListItemDto
{
    /// <summary>Biểu ghi MARC dạng JSON, đúng dạng trình soạn thảo dùng.</summary>
    public string MarcJson { get; set; } = string.Empty;

    /// <summary>Mô tả ISBD theo từng vùng, dùng cho tab "Thông tin thư mục".</summary>
    public List<IsbdAreaDto> Isbd { get; set; } = new();

    public string? StatementOfResponsibility { get; set; }
    public string? UniformTitle { get; set; }
    public string? Issn { get; set; }
    public string? PublishPlace { get; set; }
    public string? Edition { get; set; }
    public string? Pages { get; set; }
    public string? Dimensions { get; set; }
    public string? Abstract { get; set; }
    public string? SeriesTitle { get; set; }
    public string? SeriesVolume { get; set; }
    public string? SourceRef { get; set; }

    public Guid? DocumentTypeId { get; set; }
    public Guid? CarrierTypeId { get; set; }
    public Guid? LanguageId { get; set; }
    public Guid? CountryId { get; set; }

    public List<BibAuthorDto> Authors { get; set; } = new();
    public List<string> Subjects { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public List<BibClassificationDto> Classifications { get; set; } = new();
    public List<Guid> CollectionIds { get; set; } = new();

    public int VersionCount { get; set; }
    public int LoanCount { get; set; }
    public int ViewCount { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
}

public record IsbdAreaDto(string Label, string Content);

public class BibAuthorDto
{
    public Guid AuthorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool IsMain { get; set; }
}

public class BibClassificationDto
{
    public Guid ClassificationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
}

/// <summary>Một phiên bản cũ của biểu ghi.</summary>
public class BibVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeNote { get; set; }
    public string? ChangedByName { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

/// <summary>Một thay đổi giữa hai phiên bản của biểu ghi.</summary>
public class MarcDiffLineDto
{
    /// <summary>Added | Removed | Changed | Unchanged</summary>
    public string Kind { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string? Before { get; set; }
    public string? After { get; set; }
}

/// <summary>Bộ lọc của màn hình danh sách biểu ghi.</summary>
public class BibListRequest : PagedRequest
{
    public Guid? DocumentTypeId { get; set; }
    public Guid? LanguageId { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? CollectionId { get; set; }
    /// <summary>Lọc theo một giá trị của danh mục tự tạo (II.9).</summary>
    public Guid? CustomIndexValueId { get; set; }
    public Guid? PublisherId { get; set; }
    public RecordStatus? Status { get; set; }
    public BibSource? Source { get; set; }
    public int? PublishYearFrom { get; set; }
    public int? PublishYearTo { get; set; }
    /// <summary>Chỉ lấy biểu ghi chưa có đăng ký cá biệt nào — dùng để tìm biểu ghi còn thiếu ĐKCB.</summary>
    public bool? WithoutItems { get; set; }
    /// <summary>Chỉ lấy biểu ghi còn bản sẵn sàng cho mượn.</summary>
    public bool? AvailableOnly { get; set; }
}
