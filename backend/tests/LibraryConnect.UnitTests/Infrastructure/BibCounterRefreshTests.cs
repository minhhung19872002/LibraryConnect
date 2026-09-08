using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Đổi trạng thái một bản in ở đâu thì chỗ ấy phải đếm lại con số chép sẵn của biểu ghi.
///
/// `bib_records.available_item_count` là con số trang tra cứu in ra dòng "còn N bản rảnh", và là
/// con số bộ lọc "chỉ hiện tài liệu còn bản rảnh" chạy trên. Nó không tự tính: mỗi lối đổi
/// `item.Status` phải gọi <c>BibItemCounter.RefreshAsync</c>. Trước 08/09/2026 cả tầng lưu thông
/// không gọi lần nào — ghi mượn xong bản in sang "Đang mượn" mà biểu ghi vẫn nói còn rảnh.
///
/// Cùng lúc ấy phép tính này tồn tại **bốn bản chép** ở bốn tệp; luật thứ hai dưới đây cấm bản thứ
/// năm, vì bốn bản là bốn chỗ để lệch nhau.
/// </summary>
public class BibCounterRefreshTests
{
    /// <summary>Tệp đổi trạng thái bản in mà cố ý không đếm lại — mỗi dòng phải nói vì sao.</summary>
    private static readonly Dictionary<string, string> Mien = new()
    {
        ["BibImportRunner.cs"] =
            "dựng bản in mới trong lượt nhập hàng loạt; bộ nhập gọi đếm lại một lượt ở cuối lô",
    };

    [Fact]
    public void Moi_cho_doi_trang_thai_ban_in_deu_dem_lai_so_ban_ranh()
    {
        var thieu = new List<string>();

        foreach (var tep in TapTinNguon())
        {
            var noiDung = File.ReadAllText(tep);
            var ten = Path.GetFileName(tep);

            if (!Regex.IsMatch(noiDung, @"Status\s*=\s*ItemStatus\.")) continue;
            if (Mien.ContainsKey(ten)) continue;

            // Đếm lại có thể gọi thẳng, hoặc qua một hàm bọc trong cùng tệp.
            if (noiDung.Contains("BibItemCounter.", StringComparison.Ordinal)) continue;
            if (noiDung.Contains("RefreshCountsAsync", StringComparison.Ordinal)) continue;

            thieu.Add(ten);
        }

        string.Join(", ", thieu).Should().BeEmpty(
            "đổi trạng thái bản in mà không đếm lại là để trang tra cứu nói \"còn N bản rảnh\" "
            + "trong khi kho không còn bản nào");
    }

    [Fact]
    public void Phep_dem_so_ban_ranh_chi_viet_o_mot_cho()
    {
        // Bốn bản chép của cùng một câu truy vấn từng sống song song: hai bản ở Biên mục, một ở
        // Ấn phẩm định kỳ, một ở Bổ sung. Bản nào cũng đúng — cho tới ngày một bản được sửa.
        var noiKhac = TapTinNguon()
            .Where(tep => !Path.GetFileName(tep).Equals("ItemLifecycleFeatures.cs", StringComparison.Ordinal))
            .Where(tep => Regex.IsMatch(File.ReadAllText(tep),
                @"SetProperty\(\s*\w+\s*=>\s*\w+\.AvailableItemCount"))
            .Select(Path.GetFileName)
            .ToList();

        string.Join(", ", noiKhac!).Should().BeEmpty(
            "chỉ BibItemCounter được viết thẳng vào cột số bản rảnh; chỗ khác gọi nó");
    }

    private static IEnumerable<string> TapTinNguon()
    {
        var goc = GocKhoMa();

        return Directory.EnumerateFiles(Path.Combine(goc, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(tep => !tep.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal));
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (Directory.Exists(Path.Combine(thuMuc.FullName, "src", "LibraryConnect.Application")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy gốc kho mã backend.");
    }
}
