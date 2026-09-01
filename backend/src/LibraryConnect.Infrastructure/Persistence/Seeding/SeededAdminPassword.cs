using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.Users;
using Microsoft.Extensions.Configuration;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

/// <summary>
/// Mật khẩu của tài khoản quản trị được tạo ở lần khởi động đầu tiên.
///
/// Bản trước đặt một mật khẩu cố định trong mã nguồn rồi in nó ra `README.md`. Kho mã để công khai,
/// nên chuỗi ấy mở được cửa của **mọi bản cài chưa ai đăng nhập lần nào** — không phải chỉ máy của
/// người viết. Nay mỗi bản cài sinh riêng một mật khẩu, hiện đúng một lần trong nhật ký khởi động
/// của máy chủ và chỉ sống tới lần đăng nhập đầu tiên.
///
/// Vẫn khai trước được bằng biến môi trường <c>LC_SEED_ADMIN_PASSWORD</c> cho những nơi cài đặt tự
/// động bằng kịch bản, nơi không ai ngồi đọc nhật ký container.
/// </summary>
public static class SeededAdminPassword
{
    /// <summary>Khoá cấu hình; biến môi trường tương ứng là <c>LC_SEED_ADMIN_PASSWORD</c>.</summary>
    public const string ConfigurationKey = "SEED_ADMIN_PASSWORD";

    /// <summary>
    /// Quy tắc riêng cho mật khẩu này, chặt hơn quy tắc chung của hệ thống.
    ///
    /// Không đọc quy tắc trong bảng tham số vì tài khoản quản trị được tạo **trước** khi bảng tham
    /// số có dữ liệu. Mà cũng không nên đọc: đây là mật khẩu do máy sinh và người chỉ chép lại một
    /// lần, nên dài thêm không phiền ai, trong khi nó là chìa khoá của cả hệ thống.
    /// </summary>
    private static readonly PasswordPolicy Manh = new()
    {
        MinLength = 16,
        RequireUppercase = true,
        RequireLowercase = true,
        RequireDigit = true,
        RequireSpecialCharacter = true,
    };

    /// <summary>
    /// Mật khẩu sẽ dùng, kèm việc nó tới từ đâu.
    ///
    /// <paramref name="FromConfiguration"/> quyết định có in ra nhật ký hay không: mật khẩu do người
    /// cài tự khai thì họ đã biết rồi, in thêm chỉ tổ để lại dấu vết trong nhật ký máy chủ.
    /// </summary>
    public record Ket(string Password, bool FromConfiguration);

    public static Ket Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var khai = configuration[ConfigurationKey];

        return string.IsNullOrWhiteSpace(khai)
            ? new Ket(TemporaryPasswordGenerator.Generate(Manh), false)
            : new Ket(khai.Trim(), true);
    }
}
