using System.Net;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Infrastructure.Services;

namespace LibraryConnect.Api.Middleware;

/// <summary>
/// Chặn mọi lượt gọi của tài khoản đang bị buộc đổi mật khẩu, trừ chính đường đổi mật khẩu.
///
/// Giao diện đã đưa người dùng tới màn hình đổi mật khẩu ngay sau khi đăng nhập, nhưng đó chỉ là
/// phía trình duyệt: mã thông hành cấp ở lần đăng nhập ấy vẫn gọi thẳng API được. Mà mật khẩu tạm
/// của tài khoản quản trị lại nằm công khai trong tài liệu cài đặt, nên chỗ hở này là thật.
/// </summary>
public class PasswordChangeRequiredMiddleware
{
    /// <summary>Những đường phải để mở, nếu không người dùng không có cách nào đổi mật khẩu.</summary>
    private static readonly string[] Allowed =
    {
        // Đăng nhập cũng phải để mở: máy khách thường vẫn còn mã thông hành cũ trong đầu đề khi gọi
        // đăng nhập lần nữa, chặn ở đây thì người dùng không đăng nhập lại được sau khi vừa đổi.
        "/api/auth/login",
        "/api/auth/change-password",
        "/api/auth/logout",
        "/api/auth/refresh",
        "/api/auth/me",
        "/api/reader/auth/login",
        "/api/reader/auth/change-password",
        "/api/reader/auth/refresh",
        "/health"
    };

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.User;

        if (user.Identity?.IsAuthenticated == true
            && user.HasClaim(JwtTokenService.MustChangePasswordClaimType, "1")
            && !IsAllowed(context.Request.Path))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";

            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                "Tài khoản phải đổi mật khẩu trước khi sử dụng hệ thống."));

            return;
        }

        await _next(context);
    }

    private static bool IsAllowed(PathString path) =>
        Allowed.Any(allowed => path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase));
}
