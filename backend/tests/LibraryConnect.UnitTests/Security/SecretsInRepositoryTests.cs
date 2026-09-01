using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Security;

/// <summary>
/// Kho mã không được mang theo mật khẩu dùng được.
///
/// Lỗi đã có thật và nặng: `README.md` in mật khẩu quản trị ngay trang đầu, `docs/04` và `docs/06`
/// chép lại, còn mã nguồn thì giữ đúng chuỗi ấy làm hằng số. Kho mã để công khai, nên ai đọc cũng
/// đăng nhập được vào mọi bản cài chưa ai đăng nhập lần nào.
///
/// Phép thử này quét cả kho thay vì chặn một chỗ, vì lỗi kiểu này lây theo đường sao chép: sửa
/// README rồi vẫn còn nguyên trong tài liệu cài đặt và trong kịch bản kiểm thử.
/// </summary>
public class SecretsInRepositoryTests
{
    /// <summary>Mật khẩu quản trị mặc định của bản cũ. Còn sót ở đâu là còn dùng được ở đó.</summary>
    private const string MatKhauCu = "LibraryConnect" + "@2025";

    /// <summary>
    /// Thư mục kiểm thử được miễn: bộ dựng máy chủ kiểm thử phải đặt sẵn một mật khẩu biết trước thì
    /// mới đăng nhập được, và nó chỉ sống trong cơ sở dữ liệu dùng một lần rồi xoá.
    /// </summary>
    private static readonly string[] BoQuaThuMuc =
    {
        "node_modules", "obj", "bin", ".git", ".vs", "dist", "coverage",
        Path.Combine("backend", "tests"),
    };

    private static readonly string[] DuoiCanQuet =
    {
        ".md", ".cs", ".ts", ".tsx", ".yml", ".yaml", ".json", ".sh", ".sql", ".example", ".env",
    };

    [Fact]
    public void Khong_con_mat_khau_quan_tri_mac_dinh_nao_trong_kho_ma()
    {
        var viPham = QuetAsync(noiDung => noiDung.Contains(MatKhauCu, StringComparison.Ordinal));

        viPham.Should().BeEmpty(
            "mật khẩu này còn nằm trong kho mã công khai thì mọi bản cài chưa đăng nhập lần nào đều "
            + "mở cửa cho người lạ:\n" + string.Join('\n', viPham));
    }

    /// <summary>
    /// Tài liệu bàn giao nói *cách lấy* mật khẩu, không in ra giá trị.
    ///
    /// Đây mới là luật thật sự cần giữ. Chặn đúng một chuỗi thì lần sau đặt mật khẩu mặc định khác
    /// là lại lọt.
    /// </summary>
    [Fact]
    public void Tai_lieu_khong_in_gia_tri_mat_khau_nao()
    {
        var mau = new Regex(
            @"^\s*(?:Mật khẩu|Mat khau|Password)[^:\r\n]{0,20}:\s*(?<giatri>\S.*)$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        // Những giá trị này không phải mật khẩu: chỗ trống để người cài điền, hoặc câu mô tả.
        var chapNhan = new Regex(
            @"^(\||<|`?\$|\(|—|-|\.\.\.|xem |đọc |lấy |theo |sinh |ngẫu nhiên|do máy chủ|không |"
            + @"BẮT BUỘC|tự sinh)",
            RegexOptions.IgnoreCase);

        var viPham = new List<string>();

        foreach (var (duongDan, noiDung) in DocTaiLieu())
        {
            foreach (Match match in mau.Matches(noiDung))
            {
                var giaTri = match.Groups["giatri"].Value.Trim();

                if (chapNhan.IsMatch(giaTri))
                {
                    continue;
                }

                viPham.Add($"{duongDan} — {match.Value.Trim()}");
            }
        }

        viPham.Should().BeEmpty(
            "tài liệu phải chỉ cách lấy mật khẩu chứ không in ra giá trị:\n"
            + string.Join('\n', viPham));
    }

    // -----------------------------------------------------------------------------------------

    private static IReadOnlyList<string> QuetAsync(Func<string, bool> viPham) =>
        DocTatCa()
            .Where(tep => viPham(tep.NoiDung))
            .Select(tep => tep.DuongDan)
            .ToList();

    private static IEnumerable<(string DuongDan, string NoiDung)> DocTaiLieu() =>
        DocTatCa().Where(tep =>
            tep.DuongDan.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<(string DuongDan, string NoiDung)> DocTatCa()
    {
        var goc = GocKhoMa();

        foreach (var tep in Directory.EnumerateFiles(goc, "*", SearchOption.AllDirectories))
        {
            var tuongDoi = Path.GetRelativePath(goc, tep);

            if (BoQuaThuMuc.Any(bo => tuongDoi.Contains(bo, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!DuoiCanQuet.Contains(Path.GetExtension(tep), StringComparer.OrdinalIgnoreCase)
                && Path.GetFileName(tep) != ".env.example")
            {
                continue;
            }

            var noiDung = File.ReadAllText(tep);

            yield return (tuongDoi.Replace('\\', '/'), noiDung);
        }
    }

    /// <summary>Đi ngược lên từ thư mục chạy kiểm thử tới thư mục gốc của kho mã.</summary>
    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (File.Exists(Path.Combine(thuMuc.FullName, "docker-compose.yml"))
                && Directory.Exists(Path.Combine(thuMuc.FullName, "docs")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new InvalidOperationException(
            "Không tìm thấy thư mục gốc của kho mã tính từ " + AppContext.BaseDirectory);
    }
}
