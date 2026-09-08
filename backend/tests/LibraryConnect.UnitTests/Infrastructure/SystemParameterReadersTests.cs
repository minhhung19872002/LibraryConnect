using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Mỗi tham số hệ thống phải chỉ ra được **chỗ đọc** nó.
///
/// Màn hình Tham số hệ thống (mục I.3) hiện đúng những gì bộ gieo khai, và người quản trị bật tắt
/// cái nào cũng thấy "Đã lưu". Nhưng lưu không phải là có tác dụng: ba công tắc từng sống như vậy
/// qua nhiều đợt rà — ghi nhật ký lượt xem, danh mục tự tạo làm bộ lọc, phạm vi dữ liệu theo dạng
/// tài liệu — và đợt 22 tìm thêm bốn công tắc nữa không nơi nào đọc, trong đó có "Mở kho OAI-PMH
/// của mình": tắt xong mà thư viện khác vẫn thu hoạch được toàn bộ 12.950 biểu ghi.
///
/// Phép thử đọc thẳng tệp bộ gieo, rút ra từng khoá, rồi đòi khoá ấy xuất hiện ở đâu đó trong mã
/// nguồn máy chủ ngoài chính chỗ khai nó. Khoá ghép động lúc chạy (`$"CODE.{tên}_PREFIX"`) được
/// nhận diện theo khuôn; khoá thật sự không có chỗ đọc thì phải khai vào danh sách miễn kèm lý do.
/// </summary>
public class SystemParameterReadersTests
{
    /// <summary>Khoá cố ý không có chỗ đọc — mỗi dòng phải nói vì sao.</summary>
    private static readonly Dictionary<string, string> Mien = new();

    [Fact]
    public void Moi_tham_so_he_thong_deu_co_cho_doc()
    {
        var goc = GocKhoMa();
        var tepGieo = Path.Combine(goc, "src", "LibraryConnect.Infrastructure", "Persistence",
            "Seeding", "DatabaseSeeder.Parameters.cs");
        var tepHangSo = Path.Combine(goc, "src", "LibraryConnect.Infrastructure", "Services",
            "SystemParameterService.cs");

        var noiDungGieo = File.ReadAllText(tepGieo);
        var noiDungHangSo = File.ReadAllText(tepHangSo);

        // `public const string OpacPageSize = "OPAC.PAGE_SIZE";`
        var hangSo = Regex.Matches(noiDungHangSo, @"public const string (\w+) = ""([A-Z][A-Z0-9_.]+)"";")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        // Bộ gieo khai theo hai lối: chuỗi thẳng và qua hằng số.
        var khoa = Regex.Matches(noiDungGieo, @"new\(\s*(?:""([A-Z][A-Z0-9_.]+)""|ParameterKeys\.(\w+))")
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : hangSo.GetValueOrDefault(m.Groups[2].Value))
            .Where(k => !string.IsNullOrEmpty(k))
            .Distinct()
            .ToList()!;

        khoa.Should().HaveCountGreaterThan(100, "bộ dò khoá phải bắt được cả hai lối khai");

        var maNguon = TapTinNguon(goc)
            .Where(tep => !tep.EndsWith("DatabaseSeeder.Parameters.cs", StringComparison.Ordinal))
            .ToList();

        var ngoaiTepHangSo = string.Join('\n', maNguon
            .Where(tep => !tep.EndsWith("SystemParameterService.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        var tatCa = string.Join('\n', maNguon.Select(File.ReadAllText));

        // `$"CODE.{sequenceKey}_PREFIX"` — khoá chỉ tồn tại lúc chạy.
        var ghepDong = Regex.Matches(tatCa, @"\$""([A-Z]+)\.\{\w+\}(_[A-Z_]+)""")
            .Select(m => (Nhom: m.Groups[1].Value, Duoi: m.Groups[2].Value))
            .Distinct()
            .ToList();

        var chet = new List<string>();

        foreach (var k in khoa.Where(k => !Mien.ContainsKey(k!)))
        {
            if (ngoaiTepHangSo.Contains(k!, StringComparison.Ordinal)) continue;

            // Đọc gián tiếp qua tên hằng số của ParameterKeys.
            var ten = hangSo.FirstOrDefault(cap => cap.Value == k).Key;
            if (ten is not null && Regex.IsMatch(ngoaiTepHangSo, $@"\b{Regex.Escape(ten)}\b")) continue;

            if (ghepDong.Any(x => k!.StartsWith(x.Nhom + '.', StringComparison.Ordinal)
                                  && k.EndsWith(x.Duoi, StringComparison.Ordinal))) continue;

            chet.Add(k!);
        }

        string.Join(", ", chet).Should().BeEmpty(
            "công tắc lưu được mà không ai đọc là công tắc chết — người quản trị bật tắt, "
            + "màn hình báo đã lưu, hệ thống chạy y như cũ");
    }

    private static IEnumerable<string> TapTinNguon(string goc) =>
        Directory.EnumerateFiles(Path.Combine(goc, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(tep => !tep.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal));

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
