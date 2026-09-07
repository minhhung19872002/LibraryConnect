using System.Text.RegularExpressions;
using FluentAssertions;

namespace LibraryConnect.UnitTests.Security;

/// <summary>
/// Mỗi endpoint của giao diện quản trị phải nói rõ ai gọi được.
///
/// Lớp <c>ApiControllerBase</c> gắn sẵn <c>[Authorize]</c>, nên endpoint nào quên
/// <c>[RequirePermission]</c> thì **mọi tài khoản đăng nhập được** đều gọi được — kể cả thẻ đăng nhập
/// của bạn đọc, thứ mà bất kỳ ai có thẻ thư viện đều lấy được. Ngày 07/09/2026,
/// <c>GET /api/staff/options</c> vì thế trả danh sách cán bộ kèm **tên đăng nhập** cho một tài khoản
/// bạn đọc vừa lập xong — đúng thứ cần có trước khi đi dò mật khẩu. Chuông thông báo ngay bên cạnh
/// thì chặn đúng, vì bộ xử lý của nó tự hỏi "tài khoản này có phải cán bộ không".
///
/// Phép thử quét mã nguồn này không đọc được thuộc tính lúc chạy, nên nó làm việc rẻ hơn mà đủ dùng:
/// bắt mọi endpoint mới phải chọn một trong ba lối — khai <c>[RequirePermission]</c>, khai
/// <c>[AllowAnonymous]</c>, hoặc ghi tên vào danh sách dưới đây kèm lý do. Ba lối đều buộc người viết
/// dừng lại một nhịp mà nghĩ; quên thì phép thử đỏ.
/// </summary>
public class EndpointAuthorisationTests
{
    /// <summary>
    /// Endpoint tự canh quyền bên trong bộ xử lý thay vì bằng thuộc tính. Thêm dòng nào vào đây thì
    /// phải kèm lý do, và phải chắc là bộ xử lý ấy có chặn thật.
    /// </summary>
    private static readonly Dictionary<string, string> TuCanhTrongBoXuLy = new()
    {
        ["AuthController.cs POST /auth/logout"] = "Ai đăng nhập cũng đăng xuất được — không có gì để lộ.",
        ["AuthController.cs POST /auth/change-password"] = "Đổi mật khẩu của chính mình.",
        ["AuthController.cs GET /auth/me"] = "Trả về chính hồ sơ của người đang gọi.",
        ["NotificationsController.cs GET /notifications"] =
            "Bộ xử lý đòi UserId của cán bộ; tài khoản bạn đọc nhận 403.",
        ["NotificationsController.cs POST /notifications/{id:guid}/read"] =
            "Bộ xử lý đòi UserId của cán bộ.",
        ["NotificationsController.cs POST /notifications/read-all"] =
            "Bộ xử lý đòi UserId của cán bộ.",
        ["StaffController.cs GET /staff/options"] =
            "Bộ xử lý đòi UserId của cán bộ — thêm ngày 07/09/2026 sau khi thẻ bạn đọc đọc được "
            + "danh sách cán bộ kèm tên đăng nhập.",
    };

    [Fact]
    public void Moi_endpoint_quan_tri_deu_khai_ro_ai_goi_duoc()
    {
        var thuMuc = Path.Combine(GocKhoMa(), "backend", "src", "LibraryConnect.Api", "Controllers");

        Directory.Exists(thuMuc).Should().BeTrue("phải quét đúng thư mục: {0}", thuMuc);

        var thieu = new List<string>();

        foreach (var duongDan in Directory.GetFiles(thuMuc, "*.cs").OrderBy(x => x))
        {
            var ten = Path.GetFileName(duongDan);
            var noiDung = File.ReadAllText(duongDan);
            var dong = noiDung.Split('\n');

            // [AllowAnonymous] đặt ở đầu lớp thì cả controller là lối công khai.
            var truocLop = noiDung.Split("public class")[0];

            if (truocLop.Contains("[AllowAnonymous]"))
            {
                continue;
            }

            var duongDanLop = string.Empty;

            for (var i = 0; i < dong.Length; i++)
            {
                var route = Regex.Match(dong[i], @"\[Route\(""([^""]+)""\)\]");

                if (route.Success)
                {
                    duongDanLop = route.Groups[1].Value;
                }

                var verb = Regex.Match(dong[i], @"\[Http(Get|Post|Put|Delete|Patch)(?:\(""([^""]*)""\))?\]");

                if (!verb.Success)
                {
                    continue;
                }

                var quanh = string.Join('\n', dong.Skip(Math.Max(0, i - 6)).Take(14));

                if (quanh.Contains("RequirePermission") || quanh.Contains("AllowAnonymous"))
                {
                    continue;
                }

                var sub = verb.Groups[2].Value;
                var duong = '/' + duongDanLop.Replace("api/", string.Empty).Trim('/')
                            + (string.IsNullOrEmpty(sub) ? string.Empty : '/' + sub.Trim('/'));

                // Nhóm /api/reader/* là API của ứng dụng bạn đọc: từng bộ xử lý tự lấy hồ sơ bạn đọc
                // đang đăng nhập, và tài khoản cán bộ gọi vào nhận 403 "dành cho tài khoản bạn đọc".
                if (duong.StartsWith("/reader/", StringComparison.Ordinal))
                {
                    continue;
                }

                var khoa = $"{ten} {verb.Groups[1].Value.ToUpperInvariant()} {duong}";

                if (!TuCanhTrongBoXuLy.ContainsKey(khoa))
                {
                    thieu.Add(khoa);
                }
            }
        }

        thieu.Should().BeEmpty(
            "endpoint nào không khai [RequirePermission] thì mọi tài khoản đăng nhập được — kể cả thẻ "
            + "bạn đọc — đều gọi được. Hoặc khai quyền, hoặc ghi vào danh sách tự canh kèm lý do.");
    }

    private static string GocKhoMa()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            if (File.Exists(Path.Combine(thuMuc.FullName, "docker-compose.yml"))
                && Directory.Exists(Path.Combine(thuMuc.FullName, "backend")))
            {
                return thuMuc.FullName;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy thư mục gốc của kho mã.");
    }
}
