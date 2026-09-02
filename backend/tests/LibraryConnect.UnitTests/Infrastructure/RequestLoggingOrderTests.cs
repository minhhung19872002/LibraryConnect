using FluentAssertions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Bộ ghi nhật ký yêu cầu phải đứng <b>trước</b> bộ xử lý ngoại lệ trong đường ống.
///
/// Đặt sau thì nó nhìn thấy ngoại lệ nghiệp vụ bay ngang qua trước khi bộ xử lý ngoại lệ kịp đổi
/// nó thành 400/401/404 — và ghi "ERR … trả về 500" kèm cả vết ngăn xếp cho một lượt đăng nhập sai
/// mật khẩu. Đo trên máy đang chạy: 5 dòng "trả về 500" trong nhật ký mà không có lượt nào trả 500
/// thật. Lỗi 500 thật chìm trong đó, không ai thấy.
///
/// Đường ống được lắp trong <c>Program.cs</c>, không có điểm nào để cắm phép thử tích hợp vào, nên
/// phép thử quét thẳng mã nguồn — đúng cách kho mã này vẫn chặn cả một lớp lỗi.
/// </summary>
public class RequestLoggingOrderTests
{
    [Fact]
    public void Nhat_ky_yeu_cau_dung_truoc_bo_xu_ly_ngoai_le()
    {
        var program = File.ReadAllText(Path.Combine(
            GocKhoMa(), "backend", "src", "LibraryConnect.Api", "Program.cs"));

        var ghiNhatKy = program.IndexOf("UseSerilogRequestLogging", StringComparison.Ordinal);
        var xuLyNgoaiLe = program.IndexOf("UseMiddleware<ExceptionHandlingMiddleware>", StringComparison.Ordinal);

        ghiNhatKy.Should().BePositive("Program.cs phải có bộ ghi nhật ký yêu cầu");
        xuLyNgoaiLe.Should().BePositive("Program.cs phải có bộ xử lý ngoại lệ");

        ghiNhatKy.Should().BeLessThan(
            xuLyNgoaiLe,
            "bộ ghi nhật ký yêu cầu đứng sau bộ xử lý ngoại lệ thì mọi lỗi 400/401/404 đều bị ghi "
            + "thành ERR 500 kèm vết ngăn xếp, và lỗi 500 thật chìm trong đó");
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
