using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Persistence.Seeding;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Đọc bộ định nghĩa MARC 21 chuẩn nhúng trong bản build và trả về dạng tầng Application dùng được.
///
/// Dùng chung đúng một nguồn với bộ nạp dữ liệu lúc cài đặt: hai nơi đọc hai tệp khác nhau là hai
/// nơi để lệch nhau.
/// </summary>
public class MarcStandardFields : IMarcStandardFields
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public IReadOnlyList<MarcStandardField> Load() =>
        DatabaseSeeder.LoadFieldDefinitions()
            .Select(field => new MarcStandardField(
                field.Tag,
                field.Name,
                field.NameEn,
                field.Description,
                field.IsControl,
                field.IsRepeatable,
                field.IsRequired,
                field.IsRecommended,
                field.SortOrder,
                field.Indicators.Count == 0 ? null : JsonSerializer.Serialize(field.Indicators, Options),
                field.Subfields.Count == 0 ? null : JsonSerializer.Serialize(field.Subfields, Options)))
            .ToList();
}
