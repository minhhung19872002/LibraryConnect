using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace LibraryConnect.Api.Middleware;

/// <summary>
/// Cho máy chủ biết proxy nào được phép khai địa chỉ của người dùng.
///
/// ASP.NET Core chỉ tin tiêu đề <c>X-Forwarded-For</c> khi kết nối tới từ một proxy nó biết, và mặc
/// định nó chỉ biết <c>127.0.0.1</c>. Nginx trong Docker đứng ở <c>192.168.0.x</c>, nên tiêu đề bị bỏ
/// qua và mọi yêu cầu đều mang địa chỉ của Nginx. Đo trên máy đang chạy: 96.737 dòng nhật ký hệ
/// thống cùng một IP <c>192.168.0.1</c>, chữ chìm trên tài liệu số in địa chỉ của proxy, và — nặng
/// nhất — bộ giới hạn tốc độ chia ngăn theo IP nên toàn bộ bạn đọc chung một ngăn 300 yêu cầu/phút
/// (lỗi H7 đợt rà thứ ba).
///
/// Mặc định tin ba dải địa chỉ riêng (RFC 1918) và loopback — đủ cho mọi cách dựng bằng Docker hay
/// một Nginx đặt cùng mạng nội bộ. Nơi nào proxy đứng ở địa chỉ khác thì khai
/// <c>LC_Proxy__TrustedNetworks__0=203.0.113.0/24</c>. Không mở cho mọi địa chỉ: một máy lạ gọi
/// thẳng vào cổng API mà tự khai <c>X-Forwarded-For</c> thì sẽ giả được IP trong nhật ký và lách
/// được giới hạn đăng nhập.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string ConfigurationSection = "Proxy:TrustedNetworks";

    /// <summary>Ba dải địa chỉ riêng của RFC 1918 — mạng nội bộ và mạng Docker đều nằm trong đó.</summary>
    public static readonly string[] DefaultTrustedNetworks =
    {
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
    };

    public static ForwardedHeadersOptions Build(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Chỉ lấy địa chỉ gần nhất do proxy của mình ghi thêm; các địa chỉ phía trước có thể do
            // người gọi tự bịa.
            ForwardLimit = 1,
        };

        var configured = configuration.GetSection(ConfigurationSection).Get<string[]>();
        var networks = configured is { Length: > 0 } ? configured : DefaultTrustedNetworks;

        foreach (var network in networks)
        {
            options.KnownNetworks.Add(Parse(network));
        }

        // Loopback luôn được tin, để chạy không qua Docker vẫn đúng.
        options.KnownProxies.Add(IPAddress.Loopback);
        options.KnownProxies.Add(IPAddress.IPv6Loopback);

        return options;
    }

    /// <summary>Địa chỉ này có được phép khai <c>X-Forwarded-For</c> không.</summary>
    public static bool Trusts(ForwardedHeadersOptions options, IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(address);

        return options.KnownProxies.Contains(address)
               || options.KnownNetworks.Any(network => network.Contains(address));
    }

    private static Microsoft.AspNetCore.HttpOverrides.IPNetwork Parse(string value)
    {
        var parts = value.Trim().Split('/');

        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var prefix)
            || !int.TryParse(parts[1], out var length))
        {
            throw new InvalidOperationException(
                $"Dải địa chỉ proxy '{value}' không đúng dạng địa-chỉ/độ-dài, ví dụ 192.168.0.0/16.");
        }

        return new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length);
    }
}
