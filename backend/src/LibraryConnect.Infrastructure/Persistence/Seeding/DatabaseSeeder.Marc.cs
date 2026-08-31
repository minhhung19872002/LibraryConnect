using System.Reflection;
using System.Text.Json;
using LibraryConnect.Domain.Entities.Bib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Nạp bộ định nghĩa trường MARC 21 và các mẫu biên mục.
///
/// The definitions come from an embedded JSON file rather than from code: the list is data, a
/// library may extend it through the screen, and the same file format is what the "import a MARC
/// definition set" function accepts. Seeding only adds tags that are missing, so a library's own
/// edits — a renamed field, an extra local field — survive every upgrade.
/// </summary>
public partial class DatabaseSeeder
{
    /// <summary>
    /// Dạng JSON của tệp định nghĩa dùng camelCase — giống hệt dạng lưu trong cột indicators và
    /// subfields, và giống dạng giao diện đọc, nên chỉ có một hình dạng duy nhất phải nhớ.
    /// </summary>
    private static readonly JsonSerializerOptions FieldFileOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task SeedMarcFieldsAsync(CancellationToken ct = default)
    {
        var definitions = LoadFieldDefinitions();
        var existing = await _db.MarcFieldDefinitions
            .IgnoreQueryFilters()
            .Select(field => field.Tag)
            .ToListAsync(ct);

        var known = existing.ToHashSet(StringComparer.Ordinal);
        var added = 0;

        foreach (var definition in definitions.Where(field => !known.Contains(field.Tag)))
        {
            _db.MarcFieldDefinitions.Add(new MarcFieldDefinition
            {
                Id = Guid.NewGuid(),
                Tag = definition.Tag,
                Name = definition.Name,
                NameEn = definition.NameEn,
                Description = definition.Description,
                IsControl = definition.IsControl,
                IsRepeatable = definition.IsRepeatable,
                IsRequired = definition.IsRequired,
                IsRecommended = definition.IsRecommended,
                SortOrder = definition.SortOrder,
                IsActive = true,
                Indicators = definition.Indicators.Count == 0
                    ? null
                    : JsonSerializer.Serialize(definition.Indicators, FieldFileOptions),
                Subfields = definition.Subfields.Count == 0
                    ? null
                    : JsonSerializer.Serialize(definition.Subfields, FieldFileOptions)
            });

            added++;
        }

        if (added == 0)
        {
            return;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} định nghĩa trường MARC 21", added);
    }

    /// <summary>
    /// Đọc bộ định nghĩa chuẩn kèm theo bản cài đặt. Cũng được màn hình nhập định nghĩa dùng lại
    /// khi cán bộ chọn "khôi phục bộ chuẩn".
    /// </summary>
    public static IReadOnlyList<MarcFieldDefinitionFile> LoadFieldDefinitions()
    {
        const string resource = "LibraryConnect.Infrastructure.Persistence.Seeding.Data.marc21-fields.json";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
                           ?? throw new InvalidOperationException(
                               $"Không tìm thấy tài nguyên nhúng {resource}. Bản build bị thiếu tệp định nghĩa MARC 21.");

        return JsonSerializer.Deserialize<List<MarcFieldDefinitionFile>>(stream, FieldFileOptions)
               ?? throw new InvalidOperationException("Tệp định nghĩa MARC 21 không đọc được.");
    }
}

/// <summary>Hình dạng một trường trong tệp định nghĩa MARC 21.</summary>
public class MarcFieldDefinitionFile
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public bool IsControl { get; set; }
    public bool IsRepeatable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRecommended { get; set; }
    public int SortOrder { get; set; }
    public List<MarcIndicatorFile> Indicators { get; set; } = new();
    public List<MarcSubfieldFile> Subfields { get; set; } = new();
}

public class MarcIndicatorFile
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MarcIndicatorValueFile> Values { get; set; } = new();
}

public class MarcIndicatorValueFile
{
    /// <summary>Mã chỉ thị. Khoảng trắng được ghi là "#" cho dễ đọc.</summary>
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class MarcSubfieldFile
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Repeatable { get; set; }
    public bool Required { get; set; }
}
