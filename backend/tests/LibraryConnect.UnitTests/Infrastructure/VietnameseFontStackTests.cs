using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence.Seeding;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Không bộ chữ nào trong sản phẩm được gọi tên Georgia.
///
/// Georgia có sẵn trên mọi máy Windows và là bộ chữ có chân đẹp, nên nó hay được xếp vào danh sách
/// dự phòng. Nhưng Georgia **thiếu glyph dựng sẵn của nguyên âm tiếng Việt hai dấu** — ố, ề, ắ, ữ và
/// cả họ. Trình duyệt gặp chữ thiếu thì không bỏ qua: nó tách ra thành nguyên âm một dấu cộng dấu
/// thanh rời lấy từ bộ chữ khác, rồi đặt hai phần **cạnh nhau** thay vì chồng lên nhau.
///
/// Ngày 07/09/2026, banner trang chủ của máy chủ nghiệm thu hiện đúng như thế:
/// "Tài liệu sô ́ mới cập nhật". Đo bằng canvas của trình duyệt trên chính trang ấy: ở Georgia chữ
/// "ố" rộng 60,27 px trong khi "ô" rộng 31,27 px — gần gấp đôi, đúng dấu hiệu của một dấu rời đứng
/// cạnh; ở Lora, ở 'Times New Roman' và ở bộ chữ có chân mặc định thì hai con số bằng nhau.
///
/// Chỗ hỏng thật là ảnh SVG nhúng thẳng vào địa chỉ: nó không tải được phông web nào, nên bộ chữ
/// gọi tên ở đấy là bộ chữ thật sự được dùng. Hai tệp giao diện thì Georgia đứng sau 'Lora' nên chưa
/// hỏng — nhưng chỉ cần một lần 'Lora' không tải được là mọi tiêu đề rơi xuống đúng cái bẫy ấy.
/// Yêu cầu Unicode TCVN 6909:2001 của Chương V nói về chữ lưu trữ; chữ **hiện ra** cũng phải đúng.
/// </summary>
public class VietnameseFontStackTests
{
    /// <summary>Nguyên âm tiếng Việt hai dấu — đúng nhóm chữ mà Georgia không có.</summary>
    private const string ChuHaiDau = "ố ề ắ ữ ộ ẩ";

    [Fact]
    public void Anh_minh_hoa_khong_goi_ten_bo_chu_thieu_glyph_tieng_Viet()
    {
        var uri = DemoImages.Svg(
            $"Tài liệu số mới cập nhật {ChuHaiDau}",
            "Luận văn, giáo trình đọc trực tuyến ngay trên trang",
            "#22301f",
            1200,
            400);

        var svg = Uri.UnescapeDataString(uri);

        svg.Should().NotContain("Georgia",
            "Georgia thiếu glyph dựng sẵn của ố, ề, ắ, ữ — trình duyệt tách dấu ra đứng cạnh nguyên âm");

        svg.Should().Contain("serif",
            "dòng nhan đề vẫn phải là chữ có chân, chỉ đổi sang bộ chữ có đủ chữ tiếng Việt");

        uri.Length.Should().BeLessThanOrEqualTo(DemoImages.MaxUrlLength,
            "địa chỉ dài hơn cột image_url là lượt gieo dữ liệu đổ lúc khởi động container");
    }

    [Fact]
    public void Hai_giao_dien_khong_xep_Georgia_vao_danh_sach_bo_chu_du_phong()
    {
        var goc = GocKhoMa();

        var tep = new[]
        {
            Path.Combine(goc, "frontend-admin", "src", "styles.css"),
            Path.Combine(goc, "frontend-admin", "src", "theme.ts"),
            Path.Combine(goc, "frontend-opac", "src", "styles.css"),
            Path.Combine(goc, "frontend-opac", "src", "theme.ts")
        };

        var pham = new List<string>();

        foreach (var duongDan in tep)
        {
            File.Exists(duongDan).Should().BeTrue("phải quét đúng tệp: {0}", duongDan);

            var dong = File.ReadAllLines(duongDan);

            for (var i = 0; i < dong.Length; i++)
            {
                if (dong[i].Contains("Georgia", StringComparison.OrdinalIgnoreCase))
                {
                    pham.Add($"{Path.GetFileName(Path.GetDirectoryName(duongDan))}/"
                             + $"{Path.GetFileName(duongDan)}:{i + 1}: {dong[i].Trim()}");
                }
            }
        }

        pham.Should().BeEmpty(
            "Georgia thiếu chữ tiếng Việt hai dấu; để nó trong danh sách dự phòng nghĩa là hễ phông "
            + "web không tải được thì mọi tiêu đề hiện dấu rời. Dùng 'Times New Roman' hoặc serif.");
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (File.Exists(Path.Combine(thuMuc.FullName, "docker-compose.yml"))
                && Directory.Exists(Path.Combine(thuMuc.FullName, "frontend-opac")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy thư mục gốc của kho mã.");
    }
}
