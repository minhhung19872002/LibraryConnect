using LibraryConnect.Infrastructure.Configuration;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Ghép cấu hình SMTP từ hai nguồn: nhóm tham số hệ thống "Cấu hình email SMTP" mà cán bộ sửa trên
/// màn hình Tham số (I.3), và mục <c>Smtp</c> trong appsettings/biến môi trường do người cài đặt khai.
///
/// Trước 05/09/2026 bộ gửi chỉ đọc appsettings: tám ô SMTP.* trên màn hình lưu vào cơ sở dữ liệu và
/// không nơi nào đọc ra — một công tắc chết (bài học 30). Ô nào cán bộ đã điền thì ô ấy thắng; ô để
/// trống thì rơi về appsettings, để bản cài đặt bằng biến môi trường vẫn chạy như cũ.
/// </summary>
public static class SmtpSettingsResolver
{
    public const string EnabledKey = "SMTP.ENABLED";
    public const string HostKey = "SMTP.HOST";
    public const string PortKey = "SMTP.PORT";
    public const string UseSslKey = "SMTP.USE_SSL";
    public const string UsernameKey = "SMTP.USERNAME";
    public const string PasswordKey = "SMTP.PASSWORD";
    public const string FromAddressKey = "SMTP.FROM_ADDRESS";
    public const string FromNameKey = "SMTP.FROM_NAME";

    public static readonly string[] Keys =
    {
        EnabledKey, HostKey, PortKey, UseSslKey, UsernameKey, PasswordKey, FromAddressKey, FromNameKey
    };

    /// <summary>Cấu hình hiệu lực: tham số đã điền thắng, ô trống rơi về <paramref name="fallback"/>.</summary>
    public static SmtpOptions Resolve(SmtpOptions fallback, IReadOnlyDictionary<string, string?> parameters)
    {
        string? Raw(string key) =>
            parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

        return new SmtpOptions
        {
            Enabled = ParseBool(Raw(EnabledKey)) ?? fallback.Enabled,
            Host = Raw(HostKey) ?? fallback.Host,
            Port = int.TryParse(Raw(PortKey), out var port) && port > 0 ? port : fallback.Port,
            UseSsl = ParseBool(Raw(UseSslKey)) ?? fallback.UseSsl,
            Username = Raw(UsernameKey) ?? fallback.Username,
            Password = Raw(PasswordKey) ?? fallback.Password,
            FromAddress = Raw(FromAddressKey) ?? fallback.FromAddress,
            FromName = Raw(FromNameKey) ?? fallback.FromName
        };
    }

    /// <summary>Đã bật và có địa chỉ máy chủ — điều kiện để một lá thư có thể đi.</summary>
    public static bool IsUsable(SmtpOptions options) =>
        options.Enabled && !string.IsNullOrWhiteSpace(options.Host);

    private static bool? ParseBool(string? raw) => raw?.ToLowerInvariant() switch
    {
        "true" or "1" or "yes" or "có" or "co" => true,
        "false" or "0" or "no" or "không" or "khong" => false,
        _ => null
    };
}
