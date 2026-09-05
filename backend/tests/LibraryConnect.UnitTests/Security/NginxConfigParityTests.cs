using FluentAssertions;

namespace LibraryConnect.UnitTests.Security;

/// <summary>
/// Ba tệp cấu hình Nginx phải mang cùng một bộ luật.
///
/// Kho có ba tệp cho ba cách triển khai: `nginx.conf` (phát triển), `nginx.behind-proxy.conf` (sau
/// một proxy dùng chung) và `nginx.prod.conf` (máy chủ tự cầm chứng thư). Thêm luật vào một tệp rồi
/// quên hai tệp kia là chuyện đã xảy ra hai lần trong một ngày: nhánh rẽ máy thu thập chỉ có ở hai
/// tệp đầu nên thẻ meta mất đúng ở bản triển khai thật, và chính sách nội dung thì chỉ tệp phát
/// triển có — bản chạy thật không có header chống XSS chính.
///
/// Đây đúng là bài học số 9 ở mục A.3 của CLAUDE.md, chỉ khác là giữa ba tệp cấu hình thay vì hai
/// gói frontend. Nên chặn bằng một phép thử quét, không chặn bằng trí nhớ.
/// </summary>
public class NginxConfigParityTests
{
    private static readonly string[] TepCauHinh =
    {
        "nginx.conf", "nginx.behind-proxy.conf", "nginx.prod.conf"
    };

    /// <summary>Những chuỗi phải có mặt ở **mọi** tệp, kèm lý do để người sửa sau biết vì sao.</summary>
    private static readonly (string Chuoi, string LyDo)[] LuatChung =
    {
        ("Content-Security-Policy",
            "chính sách nội dung là hàng rào chống XSS chính cho trang có trình soạn thảo WYSIWYG"),
        ("X-Content-Type-Options",
            "chặn trình duyệt tự đoán kiểu nội dung"),
        ("X-Frame-Options",
            "chặn nhúng trang quản trị vào khung của người khác"),
        ("Referrer-Policy",
            "không rò đường dẫn nội bộ sang trang ngoài"),
        ("$lc_crawler",
            "máy thu thập phải được rẽ sang API để nhận thẻ meta (IX.1); thiếu thì trang chi tiết "
            + "chỉ trả về khung rỗng của trang đơn"),
        ("resolver 127.0.0.11",
            "không ghim IP của container: dựng lại một dịch vụ là Nginx gửi tới địa chỉ cũ"),
    };

    [Fact]
    public void Moi_tep_cau_hinh_nginx_deu_mang_du_bo_luat_chung()
    {
        var thuMuc = Path.Combine(GocKhoMa(), "deploy", "nginx");
        var thieu = new List<string>();

        foreach (var ten in TepCauHinh)
        {
            var duongDan = Path.Combine(thuMuc, ten);

            File.Exists(duongDan).Should().BeTrue($"kho phải có tệp cấu hình {ten}");

            var noiDung = File.ReadAllText(duongDan);

            foreach (var (chuoi, lyDo) in LuatChung)
            {
                if (!noiDung.Contains(chuoi, StringComparison.Ordinal))
                {
                    thieu.Add($"{ten}: thiếu \"{chuoi}\" — {lyDo}");
                }
            }
        }

        thieu.Should().BeEmpty(
            "cấu hình nào thiếu luật thì bản triển khai dùng cấu hình ấy mất luật, mà nhật ký vẫn "
            + "sạch và không ai biết:\n" + string.Join('\n', thieu));
    }

    /// <summary>
    /// Tệp nào siết tần suất thì phải trả 429 kèm JSON, không phải 503 HTML mặc định của Nginx.
    ///
    /// Nghiệm thu thử ngày 05/09/2026: gõ sai mật khẩu bạn đọc vài lần liền là nhận "503 Service
    /// Temporarily Unavailable" bằng trang HTML của Nginx — trình duyệt và ứng dụng di động chờ JSON
    /// nên chỉ hiện được "máy chủ lỗi", trong khi bộ giới hạn trong API thì trả 429 tiếng Việt.
    /// </summary>
    [Fact]
    public void Tep_nao_siet_tan_suat_thi_phai_tra_429_kem_json()
    {
        var thuMuc = Path.Combine(GocKhoMa(), "deploy", "nginx");
        var thieu = new List<string>();

        foreach (var ten in TepCauHinh)
        {
            var noiDung = File.ReadAllText(Path.Combine(thuMuc, ten));

            if (!noiDung.Contains("limit_req ", StringComparison.Ordinal))
            {
                continue;
            }

            if (!noiDung.Contains("limit_req_status 429", StringComparison.Ordinal))
            {
                thieu.Add($"{ten}: có limit_req nhưng thiếu \"limit_req_status 429\" — mặc định Nginx trả 503");
            }

            if (!noiDung.Contains("error_page 429", StringComparison.Ordinal)
                || !noiDung.Contains("application/json", StringComparison.Ordinal))
            {
                thieu.Add($"{ten}: thiếu trang lỗi 429 dạng JSON — máy khách chờ JSON, không đọc được HTML của Nginx");
            }
        }

        thieu.Should().BeEmpty(string.Join('\n', thieu));
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (File.Exists(Path.Combine(thuMuc.FullName, "docker-compose.yml"))
                && Directory.Exists(Path.Combine(thuMuc.FullName, "deploy")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy thư mục gốc của kho mã.");
    }
}
