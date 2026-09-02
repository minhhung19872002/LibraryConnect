using System.Net;
using FluentAssertions;
using LibraryConnect.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Địa chỉ ghi vào nhật ký, lịch sử đăng nhập và chữ chìm phải là địa chỉ mà tầng proxy đã xác
/// nhận, không phải chữ trong tiêu đề <c>X-Forwarded-For</c> do người gọi tự viết.
///
/// Trước khi sửa, <c>CurrentUser.Ip</c> lấy thẳng giá trị đầu tiên của tiêu đề: gửi
/// <c>X-Forwarded-For: 203.0.113.9</c> từ một container khác qua Nginx là lịch sử đăng nhập ghi đúng
/// địa chỉ bịa ấy (lỗi H7 đợt rà thứ ba). Nay bộ trung gian ForwardedHeaders đã ghi địa chỉ đúng vào
/// <c>Connection.RemoteIpAddress</c>, và chỉ chỗ ấy được đọc.
/// </summary>
public class CurrentUserIpTests
{
    private static CurrentUser Build(HttpContext context) =>
        new(new HttpContextAccessor { HttpContext = context }, new ServiceCollection().BuildServiceProvider());

    [Fact]
    public void Khong_tin_tieu_de_X_Forwarded_For_tho()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.0.20");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.9, 192.168.0.20";

        Build(context).Ip.Should().Be("192.168.0.20",
            "địa chỉ đã qua bộ trung gian proxy mới đáng tin; chữ trong tiêu đề là do người gọi viết");
    }

    [Fact]
    public void Dia_chi_IPv4_anh_xa_IPv6_tra_ve_dang_IPv4()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.1.2.3");

        Build(context).Ip.Should().Be("10.1.2.3");
    }

    [Fact]
    public void Khong_co_ket_noi_thi_tra_ve_null()
    {
        Build(new DefaultHttpContext()).Ip.Should().BeNull();
    }
}
