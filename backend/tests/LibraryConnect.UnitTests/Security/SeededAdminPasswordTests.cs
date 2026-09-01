using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence.Seeding;
using Microsoft.Extensions.Configuration;

namespace LibraryConnect.UnitTests.Security;

/// <summary>
/// Mật khẩu của tài khoản quản trị đầu tiên.
///
/// Bản trước đặt một mật khẩu cố định trong mã nguồn và in thẳng nó ra `README.md` — mà kho mã thì
/// để công khai. Ai đọc trang đầu của kho mã cũng biết mật khẩu quản trị của **mọi bản cài chưa
/// đăng nhập lần nào**. Nay mỗi bản cài sinh một mật khẩu riêng, chỉ hiện đúng một lần trong nhật ký
/// khởi động của máy chủ.
/// </summary>
public class SeededAdminPasswordTests
{
    private static IConfiguration Cauhinh(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair =>
                new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

    [Fact]
    public void Khong_khai_gi_thi_moi_ban_cai_sinh_mot_mat_khau_rieng()
    {
        var mot = SeededAdminPassword.Resolve(Cauhinh());
        var hai = SeededAdminPassword.Resolve(Cauhinh());

        mot.FromConfiguration.Should().BeFalse();
        mot.Password.Should().NotBe(hai.Password,
            "hai bản cài khác nhau không được dùng chung một mật khẩu");
    }

    [Fact]
    public void Mat_khau_sinh_ra_du_manh()
    {
        var sinh = SeededAdminPassword.Resolve(Cauhinh()).Password;

        sinh.Length.Should().BeGreaterThanOrEqualTo(16);
        sinh.Should().Match(mat => mat.Any(char.IsLower));
        sinh.Should().Match(mat => mat.Any(char.IsUpper));
        sinh.Should().Match(mat => mat.Any(char.IsDigit));
        sinh.Should().Match(mat => mat.Any(character => !char.IsLetterOrDigit(character)));
    }

    [Fact]
    public void Khai_bao_qua_bien_moi_truong_thi_dung_dung_gia_tri_ay()
    {
        // Cài đặt tự động (Ansible, script bàn giao) cần biết trước mật khẩu để đăng nhập lần đầu
        // mà không phải đọc nhật ký container.
        var ket = SeededAdminPassword.Resolve(
            Cauhinh((SeededAdminPassword.ConfigurationKey, "  MatKhau@BanGiao2026  ")));

        ket.Password.Should().Be("MatKhau@BanGiao2026");
        ket.FromConfiguration.Should().BeTrue(
            "biết mật khẩu tới từ cấu hình thì máy chủ mới im lặng, không in ra nhật ký");
    }

    [Fact]
    public void Khai_bao_rong_thi_coi_nhu_khong_khai()
    {
        SeededAdminPassword.Resolve(Cauhinh((SeededAdminPassword.ConfigurationKey, "   ")))
            .FromConfiguration.Should().BeFalse();
    }
}
