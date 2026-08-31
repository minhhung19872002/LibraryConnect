using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.2 — Mẫu thẻ bạn đọc: bố cục mặt trước, mặt sau và dữ liệu đổ lên thẻ.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Một ô nội dung trên thẻ, đo bằng milimét tính từ góc trên bên trái của mặt thẻ.
///
/// Thẻ nhựa khổ CR80 chỉ rộng 85,6 mm nên mọi thứ phải đặt theo milimét chứ không theo điểm ảnh:
/// máy in thẻ nhựa in đúng khổ vật lý, lệch một milimét là chữ chạm mép thẻ.
/// </summary>
public class CardBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 40;
    public double Height { get; set; } = 6;

    /// <summary>Nguồn nội dung — xem <see cref="ReaderCardFields"/>. Bắt đầu bằng dấu nháy kép là văn bản cố định.</summary>
    public string Source { get; set; } = string.Empty;

    public string? Prefix { get; set; }
    public double FontSize { get; set; } = 8;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Uppercase { get; set; }
    /// <summary>left | center | right</summary>
    public string Align { get; set; } = "left";
    /// <summary>Mã màu chữ dạng #RRGGBB.</summary>
    public string? Color { get; set; }
    public bool Border { get; set; }
}

/// <summary>Ô ảnh trên thẻ: ảnh bạn đọc hoặc logo thư viện.</summary>
public class CardImageDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 22;
    public double Height { get; set; } = 28;
    /// <summary>photo — ảnh bạn đọc; logo — logo thư viện lấy từ tham số hệ thống.</summary>
    public string Kind { get; set; } = ReaderCardImageKinds.Photo;
    public bool Border { get; set; } = true;
}

/// <summary>Khối mã vạch hoặc QR mã hóa số thẻ, để quét ở quầy và ở cổng ra vào.</summary>
public class CardBarcodeDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 40;
    public double Height { get; set; } = 10;
    public BarcodeType Type { get; set; } = BarcodeType.Code128;
    public bool ShowText { get; set; } = true;
    public double FontSize { get; set; } = 6;
}

/// <summary>Bố cục một mặt thẻ.</summary>
public class CardFaceLayoutDto
{
    public List<CardBoxDto> Boxes { get; set; } = new();
    public List<CardImageDto> Images { get; set; } = new();
    public CardBarcodeDto? Barcode { get; set; }
    /// <summary>Màu nền mặt thẻ dạng #RRGGBB; bỏ trống là nền trắng.</summary>
    public string? BackgroundColor { get; set; }
    /// <summary>Dải màu chạy ngang phía trên mặt thẻ, kiểu thẻ sinh viên quen thuộc.</summary>
    public double HeaderBandHeight { get; set; }
    public string? HeaderBandColor { get; set; }
}

public static class ReaderCardImageKinds
{
    public const string Photo = "photo";
    public const string Logo = "logo";
}

/// <summary>Các trường kéo được lên thẻ.</summary>
public static class ReaderCardFields
{
    public const string CardNumber = "cardNumber";
    public const string FullName = "fullName";
    public const string StudentCode = "studentCode";
    public const string ReaderTypeName = "readerTypeName";
    public const string FacultyName = "facultyName";
    public const string MajorName = "majorName";
    public const string ClassName = "className";
    public const string CourseYear = "courseYear";
    public const string DateOfBirth = "dateOfBirth";
    public const string Gender = "gender";
    public const string CardIssueDate = "cardIssueDate";
    public const string CardExpireDate = "cardExpireDate";
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Address = "address";
    public const string LibraryName = "libraryName";
    public const string LibraryAddress = "libraryAddress";
    public const string LibraryPhone = "libraryPhone";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [CardNumber] = "Số thẻ",
        [FullName] = "Họ và tên",
        [StudentCode] = "Mã sinh viên",
        [ReaderTypeName] = "Loại bạn đọc",
        [FacultyName] = "Khoa",
        [MajorName] = "Ngành đào tạo",
        [ClassName] = "Lớp",
        [CourseYear] = "Khóa",
        [DateOfBirth] = "Ngày sinh",
        [Gender] = "Giới tính",
        [CardIssueDate] = "Ngày cấp thẻ",
        [CardExpireDate] = "Ngày hết hạn",
        [Email] = "Email",
        [Phone] = "Điện thoại",
        [Address] = "Địa chỉ",
        [LibraryName] = "Tên thư viện",
        [LibraryAddress] = "Địa chỉ thư viện",
        [LibraryPhone] = "Điện thoại thư viện"
    };
}

/// <summary>Mẫu thẻ bạn đọc (VI.2).</summary>
public class ReaderCardTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Mặc định là khổ CR80 của thẻ nhựa: 85,6 × 54 mm.</summary>
    public double WidthMm { get; set; } = 85.6;
    public double HeightMm { get; set; } = 54;
    public int CardsPerPage { get; set; } = 10;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PrintBack { get; set; }
    public CardFaceLayoutDto Front { get; set; } = new();
    public CardFaceLayoutDto Back { get; set; } = new();
}

/// <summary>Dữ liệu của một bạn đọc dùng để đổ lên thẻ.</summary>
public class ReaderCardDataDto
{
    public Guid ReaderId { get; set; }
    public Guid? CardId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public string? MajorName { get; set; }
    public string? ClassName { get; set; }
    public string? CourseYear { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly CardIssueDate { get; set; }
    public DateOnly CardExpireDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    /// <summary>Ảnh chân dung đã tải sẵn từ kho đối tượng; null thì ô ảnh để trống có viền.</summary>
    public byte[]? Photo { get; set; }
}

/// <summary>Thông tin thư viện in trên thẻ, lấy từ tham số hệ thống chứ không viết cứng.</summary>
public record CardLibraryInfo(string? Name, string? Address, string? Phone, byte[]? Logo);
