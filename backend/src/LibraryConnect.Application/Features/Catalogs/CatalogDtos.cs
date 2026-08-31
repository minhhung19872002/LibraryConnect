using LibraryConnect.Application.Common.Models;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>Một giá trị trong danh mục, dùng chung cho mọi bảng danh mục.</summary>
public class CatalogItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    // Only meaningful for hierarchical catalogues.
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; }

    /// <summary>Giá trị các trường riêng của từng danh mục, khóa là <c>CatalogField.Key</c>.</summary>
    public Dictionary<string, string?> Extras { get; set; } = new();

    /// <summary>Số bản ghi nghiệp vụ đang tham chiếu tới giá trị này; null khi chưa tính.</summary>
    public int? UsageCount { get; set; }
}

/// <summary>Mô tả một danh mục để giao diện tự dựng màn hình mà không cần biết trước bảng nào.</summary>
public class CatalogMetadataDto
{
    public string Code { get; set; } = string.Empty;
    public string SingularName { get; set; } = string.Empty;
    public string PluralName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsHierarchical { get; set; }
    public bool ShowCode { get; set; }
    public bool ShowNameEn { get; set; }
    public bool SupportsMerge { get; set; }
    public List<CatalogFieldDto> Fields { get; set; } = new();
}

public class CatalogFieldDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Required { get; set; }
    public bool ShowInList { get; set; }
    public List<CatalogOptionDto> Options { get; set; } = new();

    /// <summary>Với trường kiểu Reference: mã danh mục cần nạp để dựng ô chọn.</summary>
    public string? ReferenceCatalog { get; set; }
}

public record CatalogOptionDto(string Value, string Label);

public class CatalogListRequest : PagedRequest
{
    public bool? IsActive { get; set; }
    /// <summary>Lọc theo nhánh cha, chỉ dùng cho danh mục phân cấp.</summary>
    public Guid? ParentId { get; set; }
    /// <summary>Trả về toàn bộ cây thay vì một trang phẳng (danh mục phân cấp).</summary>
    public bool AsTree { get; set; }
}

/// <summary>Dữ liệu thêm mới hoặc sửa một giá trị danh mục.</summary>
public class CatalogItemInput
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ParentId { get; set; }
    public Dictionary<string, string?> Extras { get; set; } = new();
}

/// <summary>Kết quả gộp trùng: giá trị nào bị gộp vào đâu và bao nhiêu tham chiếu được chuyển.</summary>
public class CatalogMergeResultDto
{
    public string TargetName { get; set; } = string.Empty;
    public int MergedCount { get; set; }
    public int UpdatedReferences { get; set; }
    public List<string> MergedNames { get; set; } = new();
}

/// <summary>Kết quả nhập danh mục từ Excel.</summary>
public class CatalogImportResultDto
{
    public int TotalRows { get; set; }
    public int CreatedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int ErrorRows { get; set; }
    public List<CatalogImportErrorDto> Errors { get; set; } = new();
}

public class CatalogImportErrorDto
{
    public int Row { get; set; }
    public string? Column { get; set; }
    public string? Value { get; set; }
    public string Message { get; set; } = string.Empty;
}
