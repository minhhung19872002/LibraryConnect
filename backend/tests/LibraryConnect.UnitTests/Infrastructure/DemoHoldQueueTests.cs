using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Vị trí trong hàng đợi đặt giữ phải là vị trí thật, không phải một con số cho đẹp.
///
/// "Xem vị trí trong hàng đợi" là câu mục XI.2 hứa với bạn đọc, và trang cá nhân của OPAC in đúng
/// con số ấy. Bộ dữ liệu trình diễn từng đặt nó bằng `1 + index % 3` ở một chỗ và bằng hằng số `1`
/// ở chỗ kia — đo trên máy chủ nghiệm thu ngày 08/09/2026 thì 37 hàng đợi sai: có hàng đợi một
/// người báo "vị trí 2", có hàng đợi hai người cùng mang số 1, và có hàng đợi hai người mang số
/// ngược thứ tự ngày đặt.
///
/// Vị trí chỉ tính đúng được **sau khi** biết cả hàng đợi gồm những ai, nên trong bộ gieo nó phải là
/// một biến đếm chạy trên nhóm đã sắp xếp, không phải một biểu thức của chỉ số vòng lặp.
/// </summary>
public class DemoHoldQueueTests
{
    [Fact]
    public void Bo_gieo_khong_dat_vi_tri_hang_doi_bang_mot_con_so_bia()
    {
        var thuMuc = Path.Combine(GocKhoMa(), "src", "LibraryConnect.Infrastructure",
            "Persistence", "Seeding");

        var viPham = new List<string>();

        foreach (var tep in Directory.EnumerateFiles(thuMuc, "*.cs"))
        {
            foreach (Match gan in Regex.Matches(File.ReadAllText(tep), @"QueuePosition\s*=\s*([^,\n]+)"))
            {
                var giaTri = gan.Groups[1].Value.Trim();

                // `0` là chỗ giữ chỗ, `thuTu++` là biến đếm chạy trên hàng đợi đã sắp xếp.
                if (giaTri is "0" || giaTri.Contains("thuTu", StringComparison.Ordinal)) continue;

                viPham.Add($"{Path.GetFileName(tep)}: QueuePosition = {giaTri}");
            }
        }

        string.Join("; ", viPham).Should().BeEmpty(
            "vị trí hàng đợi phải đánh lại theo thứ tự đặt sau khi dựng xong cả hàng đợi");
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (Directory.Exists(Path.Combine(thuMuc.FullName, "src", "LibraryConnect.Infrastructure")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy gốc kho mã backend.");
    }
}
