using FluentAssertions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Kịch bản triển khai trên máy chủ (`deploy/scripts/gh-deploy.sh`) phải dọn ảnh cũ của chính nó.
///
/// Ngày 05/09/2026 lượt triển khai thứ 21 trong hai ngày đổ vì "no space left on device": mỗi lượt kéo
/// ba ảnh gắn tag theo mã commit (ảnh API 1,37 GB) và `docker image prune -f` chỉ dọn ảnh không tag,
/// nên 20 bản cũ nằm nguyên — 27 GB trên một ổ 96 GB dùng chung với năm ứng dụng khác. Kịch bản phải
/// giữ đúng bản đang chạy và bản ngay trước (để quay lại), còn lại xoá.
/// </summary>
public class DeployScriptTests
{
    [Fact]
    public void Kich_ban_trien_khai_don_anh_libraryconnect_cu_sau_khi_len_thanh_cong()
    {
        var path = Path.Combine(GocKhoMa(), "deploy", "scripts", "gh-deploy.sh");
        var script = File.ReadAllText(path);

        script.Should().Contain("don_anh_cu",
            "phải có bước dọn ảnh cũ có tên rõ ràng để đọc nhật ký triển khai biết nó đã chạy");
        script.Should().Contain("libraryconnect-",
            "chỉ dọn ảnh của chính sản phẩm — máy chủ dùng chung, không đụng ảnh của ứng dụng khác");
        script.Should().Contain("PREV_TAG",
            "giữ lại bản ngay trước để còn quay lại được bằng LC_IMAGE_TAG");
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
