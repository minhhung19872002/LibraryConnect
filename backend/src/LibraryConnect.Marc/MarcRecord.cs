using System.Text;
using System.Text.Json.Serialization;

namespace LibraryConnect.Marc;

/// <summary>
/// Một biểu ghi MARC 21.
///
/// The structure follows the standard exactly rather than being flattened into columns: a record is
/// a 24-character leader, a set of control fields (tags 001–009, value only) and a set of data
/// fields (tags 010–999, two indicators and repeatable subfields). Field order is significant and is
/// preserved, because a record exported to ISO 2709 has to come back byte-identical.
/// </summary>
public class MarcRecord
{
    /// <summary>Chỉ mục các trường điều khiển, tag 001–009.</summary>
    [JsonPropertyName("controlFields")]
    public List<MarcControlField> ControlFields { get; set; } = new();

    /// <summary>Các trường dữ liệu, tag 010–999.</summary>
    [JsonPropertyName("dataFields")]
    public List<MarcDataField> DataFields { get; set; } = new();

    [JsonPropertyName("leader")]
    [JsonConverter(typeof(MarcLeaderJsonConverter))]
    public MarcLeader Leader { get; set; } = new();

    /// <summary>Số kiểm soát của biểu ghi, trường 001.</summary>
    [JsonIgnore]
    public string? ControlNumber
    {
        get => GetControlField("001");
        set => SetControlField("001", value);
    }

    public string? GetControlField(string tag) =>
        ControlFields.FirstOrDefault(field => field.Tag == tag)?.Value;

    public void SetControlField(string tag, string? value)
    {
        var existing = ControlFields.FirstOrDefault(field => field.Tag == tag);

        if (value is null)
        {
            if (existing is not null)
            {
                ControlFields.Remove(existing);
            }

            return;
        }

        if (existing is not null)
        {
            existing.Value = value;
            return;
        }

        ControlFields.Add(new MarcControlField(tag, value));
        // Control fields must appear in tag order in the directory, so the list is kept sorted.
        ControlFields.Sort((left, right) => string.CompareOrdinal(left.Tag, right.Tag));
    }

    /// <summary>Mọi trường dữ liệu mang tag đã cho, theo đúng thứ tự trong biểu ghi.</summary>
    public IEnumerable<MarcDataField> GetFields(string tag) =>
        DataFields.Where(field => field.Tag == tag);

    public MarcDataField? GetField(string tag) => DataFields.FirstOrDefault(field => field.Tag == tag);

    /// <summary>
    /// Giá trị subfield đầu tiên của trường đầu tiên mang tag đã cho, ví dụ <c>GetSubfield("245", 'a')</c>.
    /// </summary>
    public string? GetSubfield(string tag, char code) => GetField(tag)?.GetSubfield(code);

    /// <summary>Mọi giá trị của một subfield trên mọi lần lặp của trường, ví dụ tất cả 650$a.</summary>
    public IEnumerable<string> GetSubfields(string tag, char code) =>
        GetFields(tag).SelectMany(field => field.GetSubfields(code));

    public MarcDataField AddField(string tag, char indicator1 = ' ', char indicator2 = ' ')
    {
        var field = new MarcDataField(tag, indicator1, indicator2);
        DataFields.Add(field);
        return field;
    }

    /// <summary>Deep copy, dùng khi hiệu đính một biểu ghi lấy từ nguồn khác mà vẫn giữ bản gốc.</summary>
    public MarcRecord Clone() => new()
    {
        Leader = new MarcLeader(Leader.ToString()),
        ControlFields = ControlFields.Select(field => new MarcControlField(field.Tag, field.Value)).ToList(),
        DataFields = DataFields.Select(field => field.Clone()).ToList()
    };

    /// <summary>Dạng văn bản thuần để đọc và so sánh khi gỡ lỗi, không phải định dạng trao đổi.</summary>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"LDR {Leader}");

        foreach (var field in ControlFields)
        {
            builder.AppendLine($"{field.Tag}     {field.Value}");
        }

        foreach (var field in DataFields)
        {
            builder.AppendLine(field.ToString());
        }

        return builder.ToString();
    }
}

/// <summary>Trường điều khiển: chỉ có giá trị, không indicator, không subfield.</summary>
public class MarcControlField
{
    public MarcControlField() { }

    public MarcControlField(string tag, string value)
    {
        Tag = tag;
        Value = value;
    }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public override string ToString() => $"{Tag}     {Value}";
}

/// <summary>Trường dữ liệu: hai indicator và danh sách subfield có thứ tự.</summary>
public class MarcDataField
{
    public MarcDataField() { }

    public MarcDataField(string tag, char indicator1 = ' ', char indicator2 = ' ')
    {
        Tag = tag;
        Indicator1 = indicator1;
        Indicator2 = indicator2;
    }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>Indicator thứ nhất. Khoảng trắng nghĩa là không xác định.</summary>
    [JsonPropertyName("ind1")]
    public char Indicator1 { get; set; } = ' ';

    [JsonPropertyName("ind2")]
    public char Indicator2 { get; set; } = ' ';

    [JsonPropertyName("subfields")]
    public List<MarcSubfield> Subfields { get; set; } = new();

    public string? GetSubfield(char code) =>
        Subfields.FirstOrDefault(subfield => subfield.Code == code)?.Value;

    public IEnumerable<string> GetSubfields(char code) =>
        Subfields.Where(subfield => subfield.Code == code).Select(subfield => subfield.Value);

    public MarcDataField AddSubfield(char code, string value)
    {
        Subfields.Add(new MarcSubfield(code, value));
        return this;
    }

    public MarcDataField Clone() => new(Tag, Indicator1, Indicator2)
    {
        Subfields = Subfields.Select(subfield => new MarcSubfield(subfield.Code, subfield.Value)).ToList()
    };

    public override string ToString()
    {
        var indicators = $"{Display(Indicator1)}{Display(Indicator2)}";
        var content = string.Concat(Subfields.Select(subfield => $"${subfield.Code}{subfield.Value}"));

        return $"{Tag} {indicators} {content}";
    }

    /// <summary>A blank indicator is conventionally printed as # so it is visible on screen.</summary>
    private static char Display(char indicator) => indicator == ' ' ? '#' : indicator;
}

public class MarcSubfield
{
    public MarcSubfield() { }

    public MarcSubfield(char code, string value)
    {
        Code = code;
        Value = value;
    }

    /// <summary>Mã subfield, một ký tự trong khoảng a–z hoặc 0–9.</summary>
    [JsonPropertyName("code")]
    public char Code { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public override string ToString() => $"${Code}{Value}";
}
