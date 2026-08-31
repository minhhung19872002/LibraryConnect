namespace LibraryConnect.Marc;

/// <summary>
/// Quy tắc của một trường MARC 21: dùng cho kiểm tra biểu ghi và cho gợi ý trên trình soạn thảo.
///
/// The rules live in the database so a library can adjust them — add a local field, mark a field
/// mandatory for its own workflow — without a code change. This type is the shape the MARC library
/// needs; the application layer maps the stored definitions onto it.
/// </summary>
public class MarcFieldRule
{
    public string Tag { get; init; } = string.Empty;

    /// <summary>Tên trường bằng tiếng Việt, dùng trong thông báo lỗi để cán bộ hiểu ngay.</summary>
    public string Name { get; init; } = string.Empty;

    public bool IsControl { get; init; }

    public bool IsRepeatable { get; init; }

    /// <summary>Thiếu trường này là lỗi, không lưu được biểu ghi.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Thiếu trường này chỉ là cảnh báo — biểu ghi vẫn lưu được.</summary>
    public bool IsRecommended { get; init; }

    public IReadOnlyList<MarcIndicatorRule> Indicators { get; init; } = Array.Empty<MarcIndicatorRule>();

    public IReadOnlyList<MarcSubfieldRule> Subfields { get; init; } = Array.Empty<MarcSubfieldRule>();

    public MarcSubfieldRule? FindSubfield(char code) =>
        Subfields.FirstOrDefault(subfield => subfield.Code == code);

    public MarcIndicatorRule? FindIndicator(int position) =>
        Indicators.FirstOrDefault(indicator => indicator.Position == position);
}

public class MarcIndicatorRule
{
    /// <summary>1 hoặc 2.</summary>
    public int Position { get; init; }

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<MarcIndicatorValue> Values { get; init; } = Array.Empty<MarcIndicatorValue>();

    /// <summary>
    /// Kiểm tra một chỉ thị có nằm trong danh sách giá trị hợp lệ không.
    /// Khoảng trắng được ghi là "#" trong bảng định nghĩa cho dễ đọc.
    /// </summary>
    public bool Accepts(char indicator)
    {
        if (Values.Count == 0)
        {
            return true;
        }

        var text = indicator == ' ' ? "#" : indicator.ToString();

        return Values.Any(value => value.Code == text || (indicator == ' ' && value.Code == " "));
    }
}

public record MarcIndicatorValue(string Code, string Label);

public class MarcSubfieldRule
{
    public char Code { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsRepeatable { get; init; }

    public bool IsRequired { get; init; }
}
