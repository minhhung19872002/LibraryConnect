namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Bốn loại phích của mục lục truyền thống (II.10).</summary>
public static class CardTypes
{
    /// <summary>Phích chính, xếp theo tên tác giả.</summary>
    public const string Main = "MAIN";

    /// <summary>Phích nhan đề, xếp theo nhan đề.</summary>
    public const string Title = "TITLE";

    /// <summary>Phích chủ đề, mỗi đề mục chủ đề một phích.</summary>
    public const string Subject = "SUBJECT";

    /// <summary>Phích phân loại, xếp theo chỉ số phân loại.</summary>
    public const string Classification = "CLASSIFICATION";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Main] = "Phích chính (tác giả)",
        [Title] = "Phích nhan đề",
        [Subject] = "Phích chủ đề",
        [Classification] = "Phích phân loại"
    };
}

/// <summary>
/// Một ô nội dung trên mẫu phích.
///
/// Position and size are in millimetres because that is how a librarian measures a card and how the
/// printer is set up; converting to points is the renderer's job, not the designer's.
/// </summary>
public class CardBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    /// <summary>
    /// Nội dung của ô. Có thể là một đường dẫn MARC (<c>245$a</c>), một trường tổng hợp
    /// (<c>isbd</c>, <c>heading</c>, <c>callNumber</c>, <c>tracings</c>), hoặc văn bản cố định
    /// nếu bắt đầu bằng dấu nháy kép.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public double FontSize { get; set; } = 9;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    /// <summary>left | center | right</summary>
    public string Align { get; set; } = "left";
    public bool Border { get; set; }
    /// <summary>Nhãn in trước nội dung, ví dụ "ĐKCB: ".</summary>
    public string? Prefix { get; set; }
}

/// <summary>Bố cục một mẫu phích.</summary>
public class CardLayoutDto
{
    public List<CardBoxDto> Boxes { get; set; } = new();
    /// <summary>Lề trong của phích, tính bằng milimét.</summary>
    public double Padding { get; set; } = 4;
    public bool ShowBorder { get; set; } = true;
}

public class CardTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>MAIN | TITLE | SUBJECT | CLASSIFICATION</summary>
    public string CardType { get; set; } = CardTypes.Main;
    public string CardTypeName { get; set; } = string.Empty;
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public CardLayoutDto Layout { get; set; } = new();
}

/// <summary>Yêu cầu in phích.</summary>
public class PrintCardsRequestDto
{
    /// <summary>Các biểu ghi được tick chọn; bỏ trống thì in theo bộ lọc.</summary>
    public List<Guid> BibIds { get; set; } = new();

    public BibListRequest? Filter { get; set; }

    public Guid? TemplateId { get; set; }

    /// <summary>Các loại phích cần in cho mỗi biểu ghi.</summary>
    public List<string> CardTypes { get; set; } = new() { Cataloging.CardTypes.Main };

    /// <summary>In nhiều phích trên một trang A4 thay vì mỗi phích một trang.</summary>
    public bool MultiplePerPage { get; set; } = true;
}
