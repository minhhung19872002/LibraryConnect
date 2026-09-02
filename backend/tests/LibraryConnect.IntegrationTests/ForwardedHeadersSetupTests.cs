using System.Net;
using FluentAssertions;
using LibraryConnect.Api.Middleware;
using Microsoft.Extensions.Configuration;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Proxy đứng trong mạng Docker phải được tin để địa chỉ người dùng đi tới được nhật ký, chữ chìm
/// và bộ giới hạn tốc độ (lỗi H7 đợt rà thứ ba: 96.737 dòng nhật ký cùng một IP của Nginx).
/// </summary>
public class ForwardedHeadersSetupTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

    [Theory]
    [InlineData("192.168.0.1")]
    [InlineData("172.18.0.5")]
    [InlineData("10.1.2.3")]
    [InlineData("127.0.0.1")]
    public void Mac_dinh_tin_proxy_trong_mang_noi_bo(string address)
    {
        var options = ForwardedHeadersSetup.Build(Config());

        ForwardedHeadersSetup.Trusts(options, IPAddress.Parse(address)).Should().BeTrue(
            "Nginx trong Docker và Nginx trên mạng nội bộ đều nằm trong ba dải RFC 1918");
    }

    [Fact]
    public void Khong_tin_dia_chi_cong_cong_la()
    {
        var options = ForwardedHeadersSetup.Build(Config());

        ForwardedHeadersSetup.Trusts(options, IPAddress.Parse("8.8.8.8")).Should().BeFalse(
            "máy lạ gọi thẳng vào cổng API mà tự khai X-Forwarded-For thì phải bị bỏ qua");
    }

    [Fact]
    public void Khai_dai_rieng_thi_thay_the_mac_dinh()
    {
        var options = ForwardedHeadersSetup.Build(Config(("Proxy:TrustedNetworks:0", "203.0.113.0/24")));

        ForwardedHeadersSetup.Trusts(options, IPAddress.Parse("203.0.113.7")).Should().BeTrue();
        ForwardedHeadersSetup.Trusts(options, IPAddress.Parse("192.168.0.1")).Should().BeFalse(
            "đã khai dải riêng thì dải mặc định không còn được tin");
        ForwardedHeadersSetup.Trusts(options, IPAddress.Loopback).Should().BeTrue("loopback luôn được tin");
    }

    [Fact]
    public void Chi_lay_mot_dia_chi_gan_nhat_trong_chuoi_forwarded()
    {
        ForwardedHeadersSetup.Build(Config()).ForwardLimit.Should().Be(1,
            "các địa chỉ đứng trước trong X-Forwarded-For là do người gọi tự viết, không tin được");
    }

    [Fact]
    public void Dai_dia_chi_sai_dang_thi_bao_ro()
    {
        var act = () => ForwardedHeadersSetup.Build(Config(("Proxy:TrustedNetworks:0", "khong-phai-dia-chi")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*192.168.0.0/16*");
    }
}
