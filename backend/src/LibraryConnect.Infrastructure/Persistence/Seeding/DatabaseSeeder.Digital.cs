using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp cây bộ sưu tập tài liệu số theo cách các thư viện đại học Việt Nam vẫn chia kho số:
    /// giáo trình và bài giảng phục vụ học tập, luận văn — luận án — đề tài là tài sản trí tuệ của
    /// trường nên mặc định hạn chế, còn tài liệu tham khảo và văn bản của trường thì mở rộng hơn.
    ///
    /// Mức truy cập mặc định của từng nhánh chỉ là điểm khởi đầu: cán bộ sửa lại được cho từng
    /// nhánh, và từng tài liệu vẫn khai riêng được.
    /// </summary>
    private async Task SeedDigitalCollectionsAsync(CancellationToken ct)
    {
        if (await _db.DigitalCollections.AnyAsync(ct))
        {
            return;
        }

        var roots = new (string Code, string Name, string Description, DigitalAccessLevel Level,
            (string Code, string Name, DigitalAccessLevel Level)[] Children)[]
        {
            ("GT", "Giáo trình", "Giáo trình chính thức của các học phần", DigitalAccessLevel.Internal,
                new[]
                {
                    ("GT-TRUONG", "Giáo trình do trường biên soạn", DigitalAccessLevel.Internal),
                    ("GT-NGOAI", "Giáo trình ngoài trường", DigitalAccessLevel.Internal),
                }),
            ("BG", "Bài giảng", "Bài giảng, slide và học liệu của giảng viên", DigitalAccessLevel.Internal,
                Array.Empty<(string, string, DigitalAccessLevel)>()),
            ("LV", "Luận văn – Luận án", "Luận văn thạc sĩ và luận án tiến sĩ", DigitalAccessLevel.Restricted,
                new[]
                {
                    ("LV-THS", "Luận văn thạc sĩ", DigitalAccessLevel.Restricted),
                    ("LV-TS", "Luận án tiến sĩ", DigitalAccessLevel.Restricted),
                }),
            ("NCKH", "Đề tài nghiên cứu khoa học", "Báo cáo đề tài các cấp và bài báo khoa học",
                DigitalAccessLevel.Restricted,
                new[]
                {
                    ("NCKH-SV", "Đề tài sinh viên", DigitalAccessLevel.Internal),
                    ("NCKH-GV", "Đề tài giảng viên", DigitalAccessLevel.Restricted),
                }),
            ("TK", "Tài liệu tham khảo", "Sách và tài liệu tham khảo dạng số", DigitalAccessLevel.Internal,
                Array.Empty<(string, string, DigitalAccessLevel)>()),
            ("VB", "Văn bản – Biểu mẫu", "Văn bản của nhà trường và biểu mẫu phục vụ bạn đọc",
                DigitalAccessLevel.Public,
                Array.Empty<(string, string, DigitalAccessLevel)>()),
        };

        var order = 0;

        foreach (var root in roots)
        {
            var parent = new DigitalCollection
            {
                Code = root.Code,
                Name = root.Name,
                Description = root.Description,
                DefaultAccessLevel = root.Level,
                SortOrder = ++order * 10,
                Level = 0,
            };

            parent.Path = parent.Id.ToString();
            _db.DigitalCollections.Add(parent);

            var childOrder = 0;

            foreach (var child in root.Children)
            {
                var node = new DigitalCollection
                {
                    Code = child.Code,
                    Name = child.Name,
                    ParentId = parent.Id,
                    DefaultAccessLevel = child.Level,
                    SortOrder = ++childOrder * 10,
                    Level = 1,
                };

                node.Path = $"{parent.Path}/{node.Id}";
                _db.DigitalCollections.Add(node);
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Đã nạp cây bộ sưu tập tài liệu số.");
    }
}
