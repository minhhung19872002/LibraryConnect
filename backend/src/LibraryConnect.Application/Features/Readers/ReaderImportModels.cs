using System.Globalization;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.4 — Nhập bạn đọc từ Excel: khai báo cột, ánh xạ cột, kiểm tra dữ liệu.
// ---------------------------------------------------------------------------------------------

/// <summary>Cách xử lý khi mã sinh viên hoặc số thẻ đã có trong hệ thống.</summary>
public enum ReaderImportDuplicateAction
{
    /// <summary>Báo lỗi dòng đó và không nhập — mặc định, an toàn nhất cho lần nhập đầu.</summary>
    Reject = 0,
    /// <summary>Bỏ qua dòng đó, giữ nguyên hồ sơ đang có.</summary>
    Skip = 1,
    /// <summary>Cập nhật hồ sơ đang có bằng dữ liệu trong tệp — dùng khi đồng bộ lại danh sách lớp.</summary>
    Update = 2
}

/// <summary>Các trường nhập được từ Excel, kèm tiêu đề cột của tệp mẫu.</summary>
public static class ReaderImportFields
{
    public const string CardNumber = "cardNumber";
    public const string StudentCode = "studentCode";
    public const string FullName = "fullName";
    public const string Gender = "gender";
    public const string DateOfBirth = "dateOfBirth";
    public const string IdCardNumber = "idCardNumber";
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Address = "address";
    public const string ReaderType = "readerType";
    public const string Faculty = "faculty";
    public const string Major = "major";
    public const string ClassName = "className";
    public const string CourseYear = "courseYear";
    public const string CardIssueDate = "cardIssueDate";
    public const string CardExpireDate = "cardExpireDate";
    public const string Note = "note";

    /// <summary>Tiêu đề cột mặc định của tệp mẫu, cũng là ánh xạ ngầm định khi cán bộ không tự ánh xạ.</summary>
    public static readonly IReadOnlyDictionary<string, string> DefaultHeaders =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CardNumber] = "Số thẻ",
            [StudentCode] = "Mã sinh viên",
            [FullName] = "Họ và tên",
            [Gender] = "Giới tính",
            [DateOfBirth] = "Ngày sinh",
            [IdCardNumber] = "Số CCCD",
            [Email] = "Email",
            [Phone] = "Điện thoại",
            [Address] = "Địa chỉ",
            [ReaderType] = "Loại bạn đọc",
            [Faculty] = "Khoa",
            [Major] = "Ngành đào tạo",
            [ClassName] = "Lớp",
            [CourseYear] = "Khóa",
            [CardIssueDate] = "Ngày cấp thẻ",
            [CardExpireDate] = "Ngày hết hạn",
            [Note] = "Ghi chú"
        };

    public static readonly IReadOnlyDictionary<string, string> Descriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CardNumber] = "Bỏ trống thì hệ thống tự sinh theo quy tắc trong Tham số hệ thống.",
            [StudentCode] = "Mã sinh viên hoặc mã cán bộ. Không được trùng.",
            [FullName] = "Bắt buộc.",
            [Gender] = "Nam / Nữ / Khác.",
            [DateOfBirth] = "Dạng ngày/tháng/năm, ví dụ 05/09/2005.",
            [IdCardNumber] = "Số căn cước công dân.",
            [Email] = "Dùng để gửi thông báo sắp đến hạn trả sách.",
            [Phone] = "Chỉ gồm chữ số và các ký tự + ( ) . -",
            [Address] = "Địa chỉ liên hệ.",
            [ReaderType] = "Tên hoặc mã loại bạn đọc đã khai trong danh mục, ví dụ Sinh viên.",
            [Faculty] = "Tên hoặc mã khoa. Chưa có thì hệ thống tạo mới nếu bật tùy chọn tương ứng.",
            [Major] = "Tên hoặc mã ngành đào tạo.",
            [ClassName] = "Mã lớp, ví dụ DH19TH1.",
            [CourseYear] = "Mã khóa, ví dụ K45.",
            [CardIssueDate] = "Bỏ trống thì lấy ngày nhập.",
            [CardExpireDate] = "Bỏ trống thì tính theo hạn thẻ của loại bạn đọc.",
            [Note] = "Ghi chú tự do."
        };
}

/// <summary>Tùy chọn cho một lần nhập bạn đọc.</summary>
public class ReaderImportOptions
{
    /// <summary>Ánh xạ trường → tiêu đề cột trong tệp. Bỏ trống thì dùng tiêu đề của tệp mẫu.</summary>
    public Dictionary<string, string> Mapping { get; set; } = new();
    public ReaderImportDuplicateAction OnDuplicate { get; set; } = ReaderImportDuplicateAction.Reject;
    /// <summary>Loại bạn đọc dùng khi cột Loại bạn đọc bỏ trống.</summary>
    public Guid? DefaultReaderTypeId { get; set; }
    /// <summary>Tự tạo khoa, ngành, lớp, khóa chưa có trong danh mục thay vì báo lỗi từng dòng.</summary>
    public bool CreateMissingCatalogs { get; set; } = true;
    /// <summary>Đặt mật khẩu tra cứu ban đầu bằng ngày sinh dạng ddMMyyyy.</summary>
    public bool SetInitialPassword { get; set; }

    public string HeaderOf(string field) =>
        Mapping.TryGetValue(field, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header
            : ReaderImportFields.DefaultHeaders[field];
}

/// <summary>Một lỗi của một dòng trong tệp nhập.</summary>
public record ReaderImportErrorDto(int Row, string? Column, string? Value, string Message);

/// <summary>Kết quả kiểm tra tệp trước khi nhập — bảng lỗi để cán bộ sửa tại chỗ.</summary>
public class ReaderImportPreviewDto
{
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    /// <summary>Tiêu đề cột đọc được trong tệp, để màn hình ánh xạ cột đổ vào danh sách chọn.</summary>
    public List<string> Headers { get; set; } = new();
    public List<ReaderImportErrorDto> Errors { get; set; } = new();
    /// <summary>Vài dòng đầu đã tách trường, cho cán bộ nhìn thấy dữ liệu vào đúng cột chưa.</summary>
    public List<ReaderImportRowDto> Sample { get; set; } = new();
}

/// <summary>Một dòng đã tách trường, dùng cho phần xem trước.</summary>
public class ReaderImportRowDto
{
    public int Row { get; set; }
    public string? CardNumber { get; set; }
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ReaderType { get; set; }
    public string? Faculty { get; set; }
    public string? Major { get; set; }
    public string? ClassName { get; set; }
    public string? CourseYear { get; set; }
    public bool HasError { get; set; }
    public bool IsExisting { get; set; }
}

/// <summary>Trạng thái một đợt nhập bạn đọc.</summary>
public class ReaderImportBatchDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public JobStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public List<ReaderImportErrorDto> Errors { get; set; } = new();
}

/// <summary>Đọc ngày từ ô Excel theo các cách người dùng Việt Nam hay gõ.</summary>
public static class ReaderDateParser
{
    private static readonly string[] Formats =
    {
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
        "yyyy-MM-dd", "yyyy/MM/dd", "dd.MM.yyyy", "ddMMyyyy"
    };

    public static bool TryParse(string? value, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();

        if (DateOnly.TryParseExact(text, Formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
        {
            return true;
        }

        // Ô định dạng Ngày của Excel có thể ra chuỗi kèm giờ; cắt phần giờ rồi thử lại.
        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("vi-VN"),
                DateTimeStyles.None, out var parsed)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            date = DateOnly.FromDateTime(parsed);
            return true;
        }

        return false;
    }
}
