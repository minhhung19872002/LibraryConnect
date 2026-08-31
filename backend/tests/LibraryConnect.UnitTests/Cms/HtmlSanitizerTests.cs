using FluentAssertions;
using LibraryConnect.Infrastructure.Services;

namespace LibraryConnect.UnitTests.Cms;

/// <summary>
/// Bộ lọc HTML cho nội dung cán bộ soạn (yêu cầu 6.4).
///
/// Nội dung này hiện nguyên vẹn cho mọi khách vào trang tra cứu, nên một thẻ lọt lưới là mã chạy
/// trên máy của tất cả bạn đọc. Mỗi phép thử dưới đây là một đường tấn công thật, không phải giả
/// định: thẻ script, thuộc tính sự kiện, giao thức javascript, khung nhúng từ nơi lạ.
/// </summary>
public class HtmlSanitizerTests
{
    private readonly HtmlSanitizerService _sanitizer = new();

    [Fact]
    public void Bo_han_the_script_nhung_giu_lai_phan_chu()
    {
        var result = _sanitizer.Sanitize("<p>Xin chào</p><script>alert('x')</script>");

        result.Should().Contain("Xin chào");
        result.Should().NotContain("script");
    }

    [Fact]
    public void Bo_thuoc_tinh_su_kien_tren_the_duoc_phep()
    {
        var result = _sanitizer.Sanitize("<img src=\"/anh.jpg\" onerror=\"alert(1)\" alt=\"Ảnh\" />");

        result.Should().Contain("/anh.jpg");
        result.Should().NotContain("onerror");
    }

    [Fact]
    public void Bo_lien_ket_dung_giao_thuc_javascript()
    {
        var result = _sanitizer.Sanitize("<a href=\"javascript:alert(1)\">bấm</a>");

        result.Should().NotContain("javascript:");
        result.Should().Contain("bấm");
    }

    [Fact]
    public void Bo_thuoc_tinh_style_vi_no_mo_ra_ca_ho_chieu_tro()
    {
        var result = _sanitizer.Sanitize(
            "<p style=\"position:fixed;opacity:0\">Đè lên nút bấm</p>");

        result.Should().NotContain("style");
        result.Should().Contain("Đè lên nút bấm");
    }

    [Fact]
    public void Giu_the_dinh_dang_va_bang_ma_trinh_soan_thao_sinh_ra()
    {
        var html = "<h2>Tiêu đề</h2><ul><li>Một</li></ul>"
                   + "<table><tbody><tr><td>Ô</td></tr></tbody></table>";

        var result = _sanitizer.Sanitize(html);

        result.Should().Contain("<h2>");
        result.Should().Contain("<li>");
        result.Should().Contain("<td>");
    }

    [Fact]
    public void Chi_giu_khung_nhung_tu_nhung_noi_da_khai_bao()
    {
        var allowed = _sanitizer.Sanitize(
            "<iframe src=\"https://www.youtube.com/embed/abc123\"></iframe>");

        var blocked = _sanitizer.Sanitize(
            "<iframe src=\"https://noi-la.example.com/nhung\"></iframe>");

        allowed.Should().Contain("youtube.com/embed/abc123");
        blocked.Should().NotContain("iframe");
    }

    [Fact]
    public void Lien_ket_mo_tab_moi_duoc_them_rel_an_toan()
    {
        // Không có rel thì trang đích điều khiển được tab gốc; cán bộ soạn tin không phải nhớ điều
        // này, nên hệ thống tự thêm.
        var result = _sanitizer.Sanitize(
            "<a href=\"https://example.com\" target=\"_blank\">ra ngoài</a>");

        result.Should().Contain("noopener");
    }

    [Fact]
    public void Boc_the_lay_phan_chu_de_sinh_tom_tat()
    {
        var text = _sanitizer.ToPlainText("<p>Dòng một</p><p>Dòng hai</p>");

        // Thẻ đóng khối phải thành khoảng trắng, nếu không hai dòng dính lại thành một từ vô nghĩa.
        text.Should().Be("Dòng một Dòng hai");
    }

    [Fact]
    public void Giai_ma_thuc_the_khi_boc_the()
    {
        _sanitizer.ToPlainText("<p>Thư viện &amp; bạn đọc</p>").Should().Be("Thư viện & bạn đọc");
    }
}
