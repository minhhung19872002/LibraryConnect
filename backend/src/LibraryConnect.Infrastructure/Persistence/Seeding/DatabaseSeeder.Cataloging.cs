using System.Text.Json;
using LibraryConnect.Domain.Entities.Bib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Nạp cấu hình biên mục ban đầu: giá trị ngầm định của trường MARC (II.1) và mẫu biên mục (II.5).
///
/// These are starting points, not fixtures: a library edits them from the screens. Seeding therefore
/// only fills an empty table, so an installation that has been configured is never overwritten by an
/// upgrade.
/// </summary>
public partial class DatabaseSeeder
{
    private static readonly JsonSerializerOptions TemplateOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task SeedCatalogingConfigAsync(CancellationToken ct = default)
    {
        await SeedFieldDefaultsAsync(ct);
        await SeedTemplatesAsync(ct);
    }

    /// <summary>
    /// Giá trị ngầm định cho biểu ghi mới. Ba giá trị đầu lấy từ tham số hệ thống nên khi thư viện
    /// đổi tên cơ quan biên mục hay ngôn ngữ mặc định thì biểu ghi mới đổi theo, không phải sửa ở
    /// hai nơi.
    /// </summary>
    private async Task SeedFieldDefaultsAsync(CancellationToken ct)
    {
        if (await _db.MarcFieldDefaults.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        var defaults = new[]
        {
            new MarcFieldDefault
            {
                Tag = "040", Subfield = "a", ParameterKey = "CATALOG.MARC_040A", SortOrder = 10
            },
            new MarcFieldDefault
            {
                Tag = "040", Subfield = "b", DefaultValue = "vie", SortOrder = 20
            },
            new MarcFieldDefault
            {
                Tag = "040", Subfield = "e", DefaultValue = "AACR2", SortOrder = 30
            },
            new MarcFieldDefault
            {
                Tag = "041", Ind1 = "0", Subfield = "a", ParameterKey = "CATALOG.DEFAULT_LANGUAGE", SortOrder = 40
            },
            new MarcFieldDefault
            {
                Tag = "044", Subfield = "a", ParameterKey = "CATALOG.DEFAULT_COUNTRY", SortOrder = 50
            },
            // 008 positions are written by number, which is how the standard addresses them.
            new MarcFieldDefault
            {
                Tag = "008", Position = 35, Length = 3, ParameterKey = "CATALOG.DEFAULT_LANGUAGE", SortOrder = 60
            },
            new MarcFieldDefault
            {
                Tag = "008", Position = 15, Length = 3, ParameterKey = "CATALOG.DEFAULT_COUNTRY", SortOrder = 70
            }
        };

        foreach (var item in defaults)
        {
            item.Id = Guid.NewGuid();
            item.IsActive = true;
            _db.MarcFieldDefaults.Add(item);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} giá trị ngầm định cho trường MARC", defaults.Length);
    }

    /// <summary>
    /// Mẫu biên mục cho ba dạng tài liệu phổ biến nhất của thư viện đại học Việt Nam.
    /// </summary>
    private async Task SeedTemplatesAsync(CancellationToken ct)
    {
        if (await _db.MarcTemplates.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        var documentTypes = await _db.DocumentTypes
            .ToDictionaryAsync(type => type.Code, type => type.Id, ct);

        var templates = new[]
        {
            new
            {
                Code = "SACH",
                Name = "Sách chuyên khảo, giáo trình",
                Description = "Khung biên mục đầy đủ cho sách in: nhan đề, tác giả, xuất bản, mô tả vật lý, "
                              + "tùng thư, phụ chú, chủ đề và ký hiệu xếp giá.",
                TypeCode = "SACH",
                Fields = new[]
                {
                    Field("020", subfields: new[] { "a", "c" }),
                    Field("082", "0", "4", new[] { "a", "2" }),
                    Field("100", "1", " ", new[] { "a", "d", "e" }),
                    Field("245", "1", "0", new[] { "a", "b", "c" }),
                    Field("250", subfields: new[] { "a" }),
                    Field("260", subfields: new[] { "a", "b", "c" }),
                    Field("300", subfields: new[] { "a", "b", "c" }),
                    Field("490", "0", " ", new[] { "a", "v" }),
                    Field("500", subfields: new[] { "a" }),
                    Field("504", subfields: new[] { "a" }),
                    Field("520", subfields: new[] { "a" }),
                    Field("650", " ", "4", new[] { "a", "x" }),
                    Field("653", subfields: new[] { "a" }),
                    Field("700", "1", " ", new[] { "a", "e" })
                }
            },
            new
            {
                Code = "LUANVAN",
                Name = "Luận văn, luận án",
                Description = "Khung biên mục cho luận văn và luận án, có sẵn trường phụ chú luận án 502.",
                TypeCode = "LUANVAN",
                Fields = new[]
                {
                    Field("082", "0", "4", new[] { "a" }),
                    Field("100", "1", " ", new[] { "a" }),
                    Field("245", "1", "0", new[] { "a", "b", "c" }),
                    Field("260", subfields: new[] { "a", "b", "c" }),
                    Field("300", subfields: new[] { "a", "b", "c" }),
                    Field("502", subfields: new[] { "b", "c", "d" }),
                    Field("504", subfields: new[] { "a" }),
                    Field("520", subfields: new[] { "a" }),
                    Field("650", " ", "4", new[] { "a" }),
                    Field("653", subfields: new[] { "a" }),
                    Field("700", "1", " ", new[] { "a", "e" })
                }
            },
            new
            {
                Code = "BAIT",
                Name = "Bài trích tạp chí",
                Description = "Khung biên mục cho bài trích: trường 773 ghi tên tạp chí, số và trang.",
                TypeCode = "BAITRICH",
                Fields = new[]
                {
                    Field("100", "1", " ", new[] { "a" }),
                    Field("245", "1", "0", new[] { "a", "c" }),
                    Field("520", subfields: new[] { "a" }),
                    Field("650", " ", "4", new[] { "a" }),
                    Field("653", subfields: new[] { "a" }),
                    Field("700", "1", " ", new[] { "a" }),
                    Field("773", "0", " ", new[] { "t", "g", "x" })
                }
            }
        };

        var added = 0;

        foreach (var template in templates)
        {
            _db.MarcTemplates.Add(new MarcTemplate
            {
                Id = Guid.NewGuid(),
                Code = template.Code,
                Name = template.Name,
                Description = template.Description,
                DocumentTypeId = documentTypes.TryGetValue(template.TypeCode, out var typeId) ? typeId : null,
                IsDefault = template.Code == "SACH",
                IsActive = true,
                Fields = JsonSerializer.Serialize(template.Fields, TemplateOptions)
            });

            added++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} mẫu biên mục", added);
    }

    private static object Field(string tag, string ind1 = " ", string ind2 = " ", string[]? subfields = null) =>
        new
        {
            tag,
            ind1,
            ind2,
            subfields = (subfields ?? new[] { "a" }).Select(code => new { code, value = string.Empty })
        };
}
