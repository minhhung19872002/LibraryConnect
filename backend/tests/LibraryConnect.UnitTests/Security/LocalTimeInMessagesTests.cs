using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Security;

/// <summary>
/// K23 (06/09/2026): câu báo khoá tài khoản in **giờ UTC** cho người dùng Việt Nam. Bạn đọc nhập sai
/// mật khẩu năm lần lúc 09:39 nhận được câu "Tài khoản tạm khóa tới 02:44 06/09/2026" — một mốc giờ
/// đã trôi qua bảy tiếng trước. Họ thử lại ngay, vẫn bị từ chối, và không hiểu vì sao.
///
/// <para>Mọi mốc thời gian đọc từ cơ sở dữ liệu đều là UTC (Npgsql chuẩn hoá `timestamptz`), nên in
/// thẳng ra là in giờ UTC. Chỗ khác trong kho mã đều gọi <c>ToLocalTime()</c>; hai chỗ này quên.</para>
///
/// <para>Phép thử quét mã nguồn: mỗi chuỗi nội suy có định dạng giờ phải hoặc gọi
/// <c>ToLocalTime()</c>, hoặc lấy từ đồng hồ hệ thống (<c>_clock.Now</c>, <c>DateTimeOffset.Now</c>)
/// vốn đã là giờ máy.</para>
/// </summary>
public class LocalTimeInMessagesTests
{
    /// <summary>Chuỗi nội suy có định dạng giờ: <c>{bieu_thuc:…HH…}</c>.</summary>
    private static readonly Regex NoiSuyGio = new(
        @"\{(?<bieu_thuc>[^{}=>]*?):(?<dinh_dang>[^{}]*HH[^{}]*)\}", RegexOptions.Compiled);

    /// <summary>Gọi <c>ToString("…HH…")</c> — lối thứ hai in giờ, hay gặp trong cột của tệp xuất.</summary>
    private static readonly Regex GoiToString = new(
        @"(?<bieu_thuc>[A-Za-z_][A-Za-z0-9_.?\[\]()]*)\.ToString\(""[^""]*HH[^""]*""\)", RegexOptions.Compiled);

    [Fact]
    public void Moi_moc_gio_hien_cho_nguoi_dung_deu_la_gio_may_chu_khong_phai_UTC()
    {
        var goc = GocKhoMa();

        var viPham = new List<string>();

        foreach (var duAn in new[] { "LibraryConnect.Application", "LibraryConnect.Api", "LibraryConnect.Infrastructure" })
        {
            var thuMuc = Path.Combine(goc, "backend", "src", duAn);

            if (!Directory.Exists(thuMuc))
            {
                continue;
            }

            foreach (var tep in Directory.EnumerateFiles(thuMuc, "*.cs", SearchOption.AllDirectories))
            {
                if (tep.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var noiDung = File.ReadAllText(tep);

                foreach (Match khop in NoiSuyGio.Matches(noiDung).Concat(GoiToString.Matches(noiDung)))
                {
                    var bieuThuc = khop.Groups["bieu_thuc"].Value;

                    // An toàn khi: đã đổi sang giờ máy, hoặc vốn lấy từ đồng hồ hệ thống (`_clock.Now`,
                    // `DateTimeOffset.Now`, hay biến cục bộ đặt tên `now` gán từ chúng), hoặc là giờ trong
                    // ngày không mang múi giờ (giờ mở cửa).
                    var anToan = bieuThuc.Contains("ToLocalTime()")
                                 || bieuThuc.Contains("Now", StringComparison.OrdinalIgnoreCase)
                                 || bieuThuc.Contains("LocalDateTime")
                                 || bieuThuc.Contains("TimeOnly")
                                 || bieuThuc.Contains("OpenTime")
                                 || bieuThuc.Contains("CloseTime");

                    if (!anToan)
                    {
                        viPham.Add($"{Path.GetFileName(tep)}: {khop.Value}");
                    }
                }
            }
        }

        // In hết ra để đọc được khi đỏ.
        viPham.Should().BeEmpty(
            "mốc giờ đọc từ cơ sở dữ liệu là UTC; in thẳng cho người dùng là lệch bảy tiếng. Vi phạm: {0}",
            string.Join(" | ", viPham));
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (Directory.Exists(Path.Combine(thuMuc.FullName, "backend", "src")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy gốc kho mã.");
    }
}
