using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.Marc;

/// <summary>
/// Một trường trong bộ định nghĩa MARC 21, dạng gửi cho giao diện.
///
/// The indicator and subfield lists are stored as JSON in one column each rather than in child
/// tables: they are always read and written whole, they are only ever read through this feature, and
/// keeping them together means the editor gets a field's entire definition in one row.
/// </summary>
public class MarcFieldDto
{
    public Guid Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public bool IsControl { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<MarcIndicatorDto> Indicators { get; set; } = new();
    public List<MarcSubfieldDto> Subfields { get; set; } = new();
}

public class MarcIndicatorDto
{
    /// <summary>1 hoặc 2.</summary>
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MarcIndicatorValueDto> Values { get; set; } = new();
}

public class MarcIndicatorValueDto
{
    /// <summary>Mã chỉ thị; khoảng trắng được ghi là "#".</summary>
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class MarcSubfieldDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Repeatable { get; set; }
    public bool Required { get; set; }
}

/// <summary>
/// Chuyển đổi giữa bản ghi trong cơ sở dữ liệu, DTO gửi cho giao diện và quy tắc mà bộ kiểm tra dùng.
/// </summary>
public static class MarcFieldMapping
{
    /// <summary>
    /// Cấu hình JSON dùng cho hai cột indicators và subfields. camelCase để trùng với dạng giao
    /// diện đọc, không thoát Unicode để đọc được tiếng Việt khi xem trực tiếp trong cơ sở dữ liệu.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static MarcFieldDto ToDto(MarcFieldDefinition entity) => new()
    {
        Id = entity.Id,
        Tag = entity.Tag,
        Name = entity.Name,
        NameEn = entity.NameEn,
        Description = entity.Description,
        IsControl = entity.IsControl,
        IsRepeatable = entity.IsRepeatable,
        IsRequired = entity.IsRequired,
        IsRecommended = entity.IsRecommended,
        IsActive = entity.IsActive,
        SortOrder = entity.SortOrder,
        Indicators = Deserialise<MarcIndicatorDto>(entity.Indicators),
        Subfields = Deserialise<MarcSubfieldDto>(entity.Subfields)
    };

    /// <summary>Chuyển định nghĩa thành quy tắc cho <see cref="MarcValidator"/>.</summary>
    public static MarcFieldRule ToRule(MarcFieldDefinition entity) => new()
    {
        Tag = entity.Tag,
        Name = entity.Name,
        IsControl = entity.IsControl,
        IsRepeatable = entity.IsRepeatable,
        IsRequired = entity.IsRequired,
        IsRecommended = entity.IsRecommended,
        Indicators = Deserialise<MarcIndicatorDto>(entity.Indicators)
            .Select(indicator => new MarcIndicatorRule
            {
                Position = indicator.Position,
                Name = indicator.Name,
                Values = indicator.Values
                    .Select(value => new MarcIndicatorValue(value.Code, value.Label))
                    .ToList()
            })
            .ToList(),
        Subfields = Deserialise<MarcSubfieldDto>(entity.Subfields)
            .Where(subfield => subfield.Code.Length > 0)
            .Select(subfield => new MarcSubfieldRule
            {
                Code = subfield.Code[0],
                Name = subfield.Name,
                IsRepeatable = subfield.Repeatable,
                IsRequired = subfield.Required
            })
            .ToList()
    };

    public static string? Serialise<T>(List<T> values) =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);

    /// <summary>
    /// Đọc cột JSON. Dữ liệu hỏng ở một trường không được làm sập cả màn hình, nên trả về danh sách
    /// rỗng thay vì ném lỗi — trường đó hiện ra như chưa khai báo chỉ thị hay trường con.
    /// </summary>
    private static List<T> Deserialise<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<T>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch (JsonException)
        {
            return new List<T>();
        }
    }
}
