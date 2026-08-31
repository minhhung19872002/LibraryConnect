using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>Abstracts the clock so business rules around due dates and fines are testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>
/// Result of a successful authentication, shared by the staff and the reader login flows.
/// <paramref name="RefreshToken"/> is handed to the client; only <paramref name="RefreshTokenHash"/>
/// is ever persisted.
/// </summary>
public record TokenPair(
    string AccessToken,
    string RefreshToken,
    string RefreshTokenHash,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public interface IJwtTokenService
{
    /// <param name="mustChangePassword">
    /// Tài khoản đang bị buộc đổi mật khẩu. Cờ đi theo mã thông hành để máy chủ chặn được mọi lượt
    /// gọi khác mà không phải hỏi lại cơ sở dữ liệu ở từng yêu cầu.
    /// </param>
    TokenPair CreateTokens(
        Guid subjectId,
        string username,
        string fullName,
        bool isReader,
        IEnumerable<string> permissions,
        bool mustChangePassword = false);
    string HashRefreshToken(string token);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    /// <summary>Invalidates every key under a prefix, e.g. all cached catalogue lists.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default);
}

/// <summary>Object storage (MinIO). Files are never written under the web root.</summary>
public interface IFileStorage
{
    Task<string> UploadAsync(string bucket, string objectName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucket, string objectName, CancellationToken ct = default);
    Task<bool> ExistsAsync(string bucket, string objectName, CancellationToken ct = default);
    Task DeleteAsync(string bucket, string objectName, CancellationToken ct = default);
    /// <summary>Time-limited direct URL, used for large downloads that should bypass the API.</summary>
    Task<string> GetPresignedUrlAsync(string bucket, string objectName, TimeSpan expiry, CancellationToken ct = default);
    Task EnsureBucketAsync(string bucket, CancellationToken ct = default);
    Task<long> GetBucketSizeAsync(string bucket, CancellationToken ct = default);
}

public record EmailMessage(IReadOnlyList<string> To, string Subject, string HtmlBody, IReadOnlyList<EmailAttachment>? Attachments = null);

public record EmailAttachment(string FileName, byte[] Content, string ContentType);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}

/// <summary>
/// Delivery channel for reader notifications. The email implementation ships in this release; the
/// FCM implementation for the mobile app plugs in behind the same interface in the next one.
/// </summary>
public interface INotificationSender
{
    string Channel { get; }
    Task SendAsync(Guid readerId, string title, string body, string? link = null, CancellationToken ct = default);
}

/// <summary>Writes audit rows that the EF interceptor cannot infer, such as logins and exports.</summary>
public interface IAuditService
{
    Task LogAsync(AuditAction action, string entity, string? entityId, string? display = null,
        object? oldValue = null, object? newValue = null, bool result = true, string? message = null,
        CancellationToken ct = default);
}

/// <summary>
/// Resolves configurable values from sys.system_parameters, with a Redis-backed cache. Every
/// customer-specific string in the product comes from here.
/// </summary>
public interface ISystemParameterService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default);
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(string groupCode, CancellationToken ct = default);
    Task InvalidateAsync(CancellationToken ct = default);
}

/// <summary>
/// Generates business codes (barcode, register number, card number, order code) from the rules
/// configured in the parameters: prefix, suffix, length and yearly reset.
/// </summary>
public interface ICodeGenerator
{
    Task<string> NextAsync(string sequenceKey, CancellationToken ct = default);
    Task<IReadOnlyList<string>> NextBatchAsync(string sequenceKey, int count, CancellationToken ct = default);
}

/// <summary>Enqueues long-running work (imports, backups, harvests) onto the Hangfire server.</summary>
public interface IBackgroundJobService
{
    string Enqueue<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall);
    string Schedule<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, TimeSpan delay);
    void AddOrUpdateRecurring<T>(string jobId, System.Linq.Expressions.Expression<Func<T, Task>> methodCall, string cronExpression);
    void RemoveRecurring(string jobId);
}

/// <summary>
/// Làm sạch HTML do cán bộ soạn trong trình soạn thảo trước khi lưu (yêu cầu 6.4).
///
/// Nội dung này được trang OPAC hiển thị nguyên vẹn cho mọi khách vào xem, nên một thẻ script lọt
/// vào bài viết là chạy trên máy của tất cả bạn đọc. Lọc ở tầng lưu chứ không ở tầng hiển thị: chỉ
/// cần một chỗ quên lọc khi hiển thị là hỏng, còn lọc lúc lưu thì trong kho không bao giờ có mã độc.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>Trả về HTML chỉ còn các thẻ và thuộc tính an toàn.</summary>
    string Sanitize(string? html);

    /// <summary>Bóc hết thẻ, lấy phần chữ — dùng cho mô tả tóm tắt và thẻ meta.</summary>
    string ToPlainText(string? html);
}
