using FluentAssertions;
using LibraryConnect.Infrastructure.Configuration;
using LibraryConnect.Infrastructure.Services;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Màn hình Tham số hệ thống có nhóm "Cấu hình email SMTP" tám ô; máy chủ thật ngày 05/09/2026 để
/// SMTP.ENABLED = false và bộ gửi thư… vẫn chỉ đọc appsettings. Cán bộ điền máy chủ thư trên màn hình
/// thì thư phải đi theo cấu hình ấy; ô để trống thì giữ cách cài đặt bằng biến môi trường.
/// </summary>
public class SmtpSettingsResolverTests
{
    private static readonly SmtpOptions AppSettings = new()
    {
        Enabled = false, Host = "", Port = 587, UseSsl = true, FromName = "LibraryConnect"
    };

    [Fact]
    public void Tham_so_da_dien_tren_man_hinh_thang_appsettings()
    {
        var parameters = new Dictionary<string, string?>
        {
            [SmtpSettingsResolver.EnabledKey] = "true",
            [SmtpSettingsResolver.HostKey] = "smtp.thuvien.edu.vn",
            [SmtpSettingsResolver.PortKey] = "465",
            [SmtpSettingsResolver.UseSslKey] = "false",
            [SmtpSettingsResolver.UsernameKey] = "thuvien",
            [SmtpSettingsResolver.PasswordKey] = "bi-mat",
            [SmtpSettingsResolver.FromAddressKey] = "thuvien@thuvien.edu.vn",
            [SmtpSettingsResolver.FromNameKey] = "Thư viện Trường"
        };

        var effective = SmtpSettingsResolver.Resolve(AppSettings, parameters);

        effective.Enabled.Should().BeTrue();
        effective.Host.Should().Be("smtp.thuvien.edu.vn");
        effective.Port.Should().Be(465);
        effective.UseSsl.Should().BeFalse();
        effective.Username.Should().Be("thuvien");
        effective.Password.Should().Be("bi-mat");
        effective.FromAddress.Should().Be("thuvien@thuvien.edu.vn");
        effective.FromName.Should().Be("Thư viện Trường");
        SmtpSettingsResolver.IsUsable(effective).Should().BeTrue();
    }

    [Fact]
    public void O_de_trong_thi_roi_ve_appsettings()
    {
        var fallback = new SmtpOptions { Enabled = true, Host = "smtp.tu-env", Port = 2525, FromName = "Từ env" };
        var parameters = new Dictionary<string, string?>
        {
            [SmtpSettingsResolver.HostKey] = "",
            [SmtpSettingsResolver.PortKey] = "không phải số",
            [SmtpSettingsResolver.EnabledKey] = null
        };

        var effective = SmtpSettingsResolver.Resolve(fallback, parameters);

        effective.Enabled.Should().BeTrue();
        effective.Host.Should().Be("smtp.tu-env");
        effective.Port.Should().Be(2525);
        effective.FromName.Should().Be("Từ env");
    }

    [Fact]
    public void Tat_tren_man_hinh_thi_tat_du_env_bat()
    {
        var fallback = new SmtpOptions { Enabled = true, Host = "smtp.tu-env" };
        var parameters = new Dictionary<string, string?> { [SmtpSettingsResolver.EnabledKey] = "false" };

        SmtpSettingsResolver.IsUsable(SmtpSettingsResolver.Resolve(fallback, parameters)).Should().BeFalse();
    }
}
