using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Cách xử lý khi biểu ghi trong tệp trùng với biểu ghi đã có (II.6 bước 2).</summary>
public enum DuplicateAction
{
    /// <summary>Bỏ qua biểu ghi trong tệp, giữ nguyên biểu ghi đã có.</summary>
    Skip = 0,

    /// <summary>Ghi đè toàn bộ biểu ghi đã có bằng biểu ghi trong tệp; bản cũ vào lịch sử phiên bản.</summary>
    Overwrite = 1,

    /// <summary>Tạo biểu ghi mới, chấp nhận có hai biểu ghi cùng tài liệu.</summary>
    CreateNew = 2,

    /// <summary>Chỉ bổ sung những trường biểu ghi đã có còn thiếu, không đụng vào trường đã có nội dung.</summary>
    Merge = 3
}

/// <summary>Căn cứ xác định trùng.</summary>
public enum DuplicateMatchBy
{
    /// <summary>Theo số ISBN đã chuẩn hóa. Chặt chẽ nhất với sách hiện đại.</summary>
    Isbn = 0,

    /// <summary>Theo số kiểm soát 001 kèm mã cơ quan 003 — dùng khi hai bên cùng một hệ thống.</summary>
    ControlNumber = 1,

    /// <summary>Theo nhan đề và tác giả đã bỏ dấu — dùng cho tài liệu cũ không có ISBN.</summary>
    TitleAndAuthor = 2
}

/// <summary>Lựa chọn của cán bộ ở các bước 2 và 3 của luồng nhập.</summary>
public class BibImportOptions
{
    public DuplicateMatchBy MatchBy { get; set; } = DuplicateMatchBy.Isbn;
    public DuplicateAction OnDuplicate { get; set; } = DuplicateAction.Skip;

    /// <summary>Dạng tài liệu gán cho mọi biểu ghi nhập vào.</summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>Trạng thái của biểu ghi sau khi nhập.</summary>
    public RecordStatus Status { get; set; } = RecordStatus.Published;

    /// <summary>Đưa biểu ghi vào hàng đợi biên mục chi tiết sau khi nhập.</summary>
    public bool AddToCatalogQueue { get; set; }

    /// <summary>Tạo sẵn đăng ký cá biệt cho mỗi biểu ghi nhập vào.</summary>
    public bool CreateItems { get; set; }

    public int ItemQuantity { get; set; } = 1;
    public Guid? WarehouseId { get; set; }
    public Guid? FundingSourceId { get; set; }

    /// <summary>Chỉ kiểm tra tệp, không ghi gì vào cơ sở dữ liệu.</summary>
    public bool DryRun { get; set; }
}

/// <summary>Một biểu ghi trong tệp, kèm kết quả đối chiếu trùng — dùng cho màn hình xem trước.</summary>
public class BibImportPreviewRow
{
    public int RecordNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public int? PublishYear { get; set; }
    public string? ControlNumber { get; set; }
    public string MarcJson { get; set; } = string.Empty;

    /// <summary>Biểu ghi đã có trùng với dòng này, nếu tìm thấy.</summary>
    public Guid? DuplicateOfId { get; set; }
    public string? DuplicateOfTitle { get; set; }
    public string? DuplicateOfControlNumber { get; set; }

    public bool HasErrors { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>Kết quả bước xem trước.</summary>
public class BibImportPreviewDto
{
    public string Format { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int DuplicateCount { get; set; }
    public int InvalidCount { get; set; }
    public List<BibImportPreviewRow> Records { get; set; } = new();
    public List<MarcFileErrorDto> FileErrors { get; set; } = new();
}

/// <summary>Trạng thái của một tác vụ nhập hoặc xuất.</summary>
public class ImportJobDto
{
    public Guid Id { get; set; }
    public ImportExportJobType Type { get; set; }
    public string? FileName { get; set; }
    public JobStatus Status { get; set; }
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public List<ImportJobErrorDto> Errors { get; set; } = new();
    public bool HasResultFile { get; set; }
}

public class ImportJobErrorDto
{
    public int Row { get; set; }
    public string? Identifier { get; set; }
    public string Message { get; set; } = string.Empty;
}
