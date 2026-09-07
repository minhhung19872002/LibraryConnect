using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Mọi danh sách có phân trang phải kết thúc chuỗi sắp xếp bằng một khóa duy nhất.
///
/// Sắp theo một cột không duy nhất — ngày lập, họ tên, thời điểm xảy ra — rồi phân trang bằng
/// LIMIT/OFFSET là giao thứ tự của các dòng bằng nhau cho PostgreSQL quyết định, mà mỗi trang là
/// một câu truy vấn riêng nên nó được phép trả lời khác nhau. Hậu quả không phải "thứ tự hơi lạ":
/// một dòng hiện hai lần ở trang sau thì có đúng một dòng khác <b>không bao giờ hiện ra ở trang
/// nào</b>. Đo trên máy chủ thật ngày 07/09/2026: danh sách tiền phạt lấy 396 dòng mà chỉ có 316
/// dòng khác nhau; danh sách bạn đọc lặp một dòng vì hai bạn đọc trùng họ tên.
///
/// Phép thử quét mã nguồn vì đây là một lớp lỗi chứ không phải một chỗ: mỗi lần thêm một danh sách
/// mới là một cơ hội quên. <c>ApplySort</c> tự gắn khóa phụ nên chuỗi nào đi qua nó là đạt; chuỗi
/// tự viết <c>OrderBy…</c> phải tự gắn <c>ThenBy(x =&gt; x.Id)</c>.
/// </summary>
public class StablePagingOrderTests
{
    /// <summary>Những chỗ sắp xếp trong bộ nhớ hoặc trên nhóm, nơi khóa chính không tồn tại.</summary>
    private static readonly IReadOnlyDictionary<string, string> ChoNgoaiLe = new Dictionary<string, string>
    {
        ["ParameterFeatures.cs"] =
            "gộp tham số theo nhóm rồi sắp; khóa của nhóm là mã nhóm, đã dùng làm khóa phụ",
    };

    private static readonly Regex ToanTuSapXep = new(
        @"\.(ApplySort|OrderBy|OrderByDescending|ThenBy|ThenByDescending)\b", RegexOptions.Compiled);

    private static readonly Regex KhoaPhuDuyNhat = new(
        @"ThenBy(Descending)?\([^)]*\.(Id|Code|Key\.\w+)\b", RegexOptions.Compiled);

    [Fact]
    public void Moi_danh_sach_phan_trang_ket_thuc_bang_mot_khoa_duy_nhat()
    {
        var thuMuc = Path.Combine(
            GocKhoMa(), "backend", "src", "LibraryConnect.Application", "Features");

        var viPham = new List<string>();

        foreach (var tep in Directory.EnumerateFiles(thuMuc, "*.cs", SearchOption.AllDirectories))
        {
            var ten = Path.GetFileName(tep);
            var noiDung = File.ReadAllText(tep);

            foreach (Match goi in Regex.Matches(noiDung, @"ToPagedResultAsync"))
            {
                // Chuỗi truy vấn dẫn tới lượt phân trang này; 1.400 ký tự đủ phủ một handler.
                var dau = Math.Max(0, goi.Index - 1400);
                var doan = noiDung[dau..goi.Index];

                var toanTu = ToanTuSapXep.Matches(doan);

                // Có handler dựng truy vấn trong một phương thức riêng (nhật ký hệ thống), nên khi
                // cửa sổ không thấy toán tử sắp xếp nào thì soi cả tệp.
                if (toanTu.Count == 0)
                {
                    doan = noiDung;
                    toanTu = ToanTuSapXep.Matches(doan);
                }

                if (toanTu.Count == 0)
                {
                    viPham.Add($"{ten}: lượt phân trang không sắp xếp gì cả");
                    continue;
                }

                if (toanTu.Any(match => match.Groups[1].Value == "ApplySort"))
                {
                    continue;
                }

                if (KhoaPhuDuyNhat.IsMatch(doan) || ChoNgoaiLe.ContainsKey(ten))
                {
                    continue;
                }

                viPham.Add(
                    $"{ten}: sắp bằng .{toanTu[^1].Groups[1].Value} mà không có khóa phụ duy nhất");
            }
        }

        viPham.Should().BeEmpty(
            "danh sách phân trang sắp theo cột không duy nhất thì trang sau lặp dòng của trang "
            + "trước, và đúng bấy nhiêu dòng khác không bao giờ hiện ra: "
            + string.Join(" | ", viPham));
    }

    /// <summary>Khóa phụ nằm trong <c>ApplySort</c>, nên chính nó phải có.</summary>
    [Fact]
    public void ApplySort_tu_gan_khoa_phu_cho_moi_lan_sap_xep()
    {
        var tep = Path.Combine(
            GocKhoMa(), "backend", "src", "LibraryConnect.Application",
            "Common", "Extensions", "QueryableExtensions.cs");

        var noiDung = File.ReadAllText(tep);

        noiDung.Should().Contain(
            "WithStableTieBreak",
            "ApplySort là đường sắp xếp của phần lớn danh sách; khóa phụ đặt ở đó thì mọi danh sách "
            + "đi qua nó đều ổn định");

        noiDung.Should().Contain(
            "ordered.ThenBy(", "khóa phụ phải thật sự được gắn thêm vào chuỗi sắp xếp");
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null && !Directory.Exists(Path.Combine(thuMuc.FullName, ".git")))
        {
            thuMuc = thuMuc.Parent;
        }

        thuMuc.Should().NotBeNull("phép thử phải chạy bên trong kho mã");

        return thuMuc!.FullName;
    }
}
