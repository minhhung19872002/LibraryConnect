using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>
/// Một ô văn bản trên tem mã vạch hoặc nhãn gáy.
///
/// Vị trí và kích thước tính bằng milimét vì đó là đơn vị của tờ tem mua sẵn và của thước cán bộ
/// dùng để đo; đổi sang điểm in là việc của bộ kết xuất, không phải của người thiết kế.
/// </summary>
public class LabelBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    /// <summary>
    /// Nguồn nội dung — xem <see cref="LabelFields"/>. Bắt đầu bằng dấu nháy kép thì là văn bản cố định.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public double FontSize { get; set; } = 7;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    /// <summary>left | center | right</summary>
    public string Align { get; set; } = "left";
    public bool Border { get; set; }
    public string? Prefix { get; set; }
}

/// <summary>Khối mã vạch trên tem.</summary>
public class LabelBarcodeDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 40;
    public double Height { get; set; } = 12;
    /// <summary>In dãy số dưới vạch để còn nhập tay được khi máy quét hỏng.</summary>
    public bool ShowText { get; set; } = true;
    public double FontSize { get; set; } = 6;
    /// <summary>Bỏ trống thì dùng loại mã của mẫu tem.</summary>
    public BarcodeType? Type { get; set; }
    /// <summary>Trường được mã hóa: barcode | registerNumber | callNumber.</summary>
    public string Source { get; set; } = LabelFields.Barcode;
}

/// <summary>Bố cục một mẫu tem hoặc nhãn.</summary>
public class LabelLayoutDto
{
    public List<LabelBoxDto> Boxes { get; set; } = new();
    /// <summary>Khối mã vạch; null với nhãn gáy thuần chữ.</summary>
    public LabelBarcodeDto? Barcode { get; set; }
    public double Padding { get; set; } = 1.5;
    public bool ShowBorder { get; set; }
}

/// <summary>Các trường có thể kéo lên tem, dùng chung cho tem mã vạch và nhãn gáy.</summary>
public static class LabelFields
{
    public const string Barcode = "barcode";
    public const string RegisterNumber = "registerNumber";
    public const string CallNumber = "callNumber";
    public const string Ddc = "ddc";
    public const string Title = "title";
    public const string Author = "author";
    public const string LibraryName = "libraryName";
    public const string WarehouseName = "warehouseName";
    public const string Isbn = "isbn";
    public const string PublishYear = "publishYear";
    public const string Price = "price";
    public const string CopyNumber = "copyNumber";
    /// <summary>Ba dòng ký hiệu xếp giá tách sẵn, kiểu nhãn gáy quen thuộc.</summary>
    public const string CallNumberLine1 = "callNumberLine1";
    public const string CallNumberLine2 = "callNumberLine2";
    public const string CallNumberLine3 = "callNumberLine3";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Barcode] = "Mã vạch",
        [RegisterNumber] = "Số ĐKCB",
        [CallNumber] = "Ký hiệu xếp giá",
        [Ddc] = "Chỉ số DDC",
        [Title] = "Nhan đề",
        [Author] = "Tác giả",
        [LibraryName] = "Tên thư viện",
        [WarehouseName] = "Tên kho",
        [Isbn] = "ISBN",
        [PublishYear] = "Năm xuất bản",
        [Price] = "Giá bìa",
        [CopyNumber] = "Số bản",
        [CallNumberLine1] = "Ký hiệu xếp giá — dòng 1",
        [CallNumberLine2] = "Ký hiệu xếp giá — dòng 2",
        [CallNumberLine3] = "Ký hiệu xếp giá — dòng 3"
    };
}

/// <summary>Mẫu tem mã vạch (III.2).</summary>
public class BarcodeTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 50;
    public double HeightMm { get; set; } = 25;
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;
    public int ColumnsPerPage { get; set; } = 4;
    public int RowsPerPage { get; set; } = 10;
    public double MarginTopMm { get; set; } = 10;
    public double MarginLeftMm { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public LabelLayoutDto Layout { get; set; } = new();
}

/// <summary>Mẫu nhãn gáy sách (III.2).</summary>
public class LabelTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 35;
    public double HeightMm { get; set; } = 45;
    public int ColumnsPerPage { get; set; } = 5;
    public int RowsPerPage { get; set; } = 6;
    public double MarginTopMm { get; set; } = 10;
    public double MarginLeftMm { get; set; } = 8;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public LabelLayoutDto Layout { get; set; } = new();
}

/// <summary>Dữ liệu của một ĐKCB dùng để đổ lên tem.</summary>
public class LabelDataDto
{
    public Guid ItemId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string RegisterNumber { get; set; } = string.Empty;
    public string? CallNumber { get; set; }
    public string? Ddc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? LibraryName { get; set; }
    public string? WarehouseName { get; set; }
    public string? Isbn { get; set; }
    public int? PublishYear { get; set; }
    public decimal Price { get; set; }
    public int CopyNumber { get; set; }
}
