using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// SMTP delivery through MailKit. Disabled by default so a deployment without a mail relay still
/// works; the outgoing message is then written to the log instead.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _fallback;
    private readonly ISystemParameterService _parameters;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpOptions> options,
        ISystemParameterService parameters,
        ILogger<SmtpEmailSender> logger)
    {
        _fallback = options.Value;
        _parameters = parameters;
        _logger = logger;
    }

    /// <summary>
    /// Cấu hình hiệu lực tại thời điểm gửi: nhóm tham số SMTP.* cán bộ sửa trên màn hình thắng, ô trống
    /// rơi về appsettings. Đọc mỗi lần gửi để đổi tham số là có tác dụng ngay (bài học 31).
    /// </summary>
    private async Task<SmtpOptions> ResolveAsync(CancellationToken ct)
    {
        var values = new Dictionary<string, string?>();

        foreach (var key in SmtpSettingsResolver.Keys)
        {
            values[key] = await _parameters.GetAsync(key, ct);
        }

        return SmtpSettingsResolver.Resolve(_fallback, values);
    }

    public async Task<bool> IsEnabledAsync(CancellationToken ct = default) =>
        SmtpSettingsResolver.IsUsable(await ResolveAsync(ct));

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var options = await ResolveAsync(ct);

        if (!SmtpSettingsResolver.IsUsable(options))
        {
            _logger.LogInformation(
                "Email sending is disabled; message \"{Subject}\" to {Recipients} was not delivered",
                message.Subject, string.Join(", ", message.To));
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(options.FromName, options.FromAddress));

        foreach (var recipient in message.To.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            mime.To.Add(MailboxAddress.Parse(recipient));
        }

        if (mime.To.Count == 0)
        {
            return;
        }

        mime.Subject = message.Subject;

        var body = new BodyBuilder { HtmlBody = message.HtmlBody };

        foreach (var attachment in message.Attachments ?? Array.Empty<EmailAttachment>())
        {
            body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }

        mime.Body = body.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port,
                options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                await client.AuthenticateAsync(options.Username, options.Password, ct);
            }

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or AuthenticationException
                                   or IOException or System.Net.Sockets.SocketException)
        {
            // Máy chủ thư sai địa chỉ hay từ chối đăng nhập là lỗi cấu hình của thư viện, không phải lỗi
            // hệ thống: nói rõ để cán bộ vào Tham số hệ thống sửa, thay vì một trang 500.
            _logger.LogWarning(ex, "Không gửi được thư \"{Subject}\" qua {Host}:{Port}", message.Subject, options.Host, options.Port);
            throw new LibraryConnect.Application.Common.Exceptions.ConflictException(
                $"Không kết nối được máy chủ thư {options.Host}:{options.Port} — kiểm tra lại Cấu hình email trong Tham số hệ thống. ({ex.Message})");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var options = await ResolveAsync(ct);

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            return false;
        }

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port,
                options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrWhiteSpace(options.Username))
            {
                await client.AuthenticateAsync(options.Username, options.Password, ct);
            }

            await client.DisconnectAsync(true, ct);
            return true;
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or AuthenticationException
                                   or IOException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "SMTP connection test failed");
            return false;
        }
    }
}

/// <summary>
/// Kênh thông báo cho bạn đọc: luôn ghi một dòng thông báo trong ứng dụng, rồi gửi email nếu có địa
/// chỉ và đẩy tới mọi thiết bị đang đăng ký (Phase 15). Bạn đọc tắt loại nào thì email và đẩy của
/// loại ấy không đi, nhưng dòng trong ứng dụng vẫn có — đó là lịch sử, không phải tiếng chuông.
/// Thiết bị mà Firebase báo không còn đăng ký thì bị đánh dấu ngừng ngay trong lượt gửi.
/// </summary>
public class ReaderNotificationSender : INotificationSender
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IPushSender _push;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ReaderNotificationSender> _logger;

    public ReaderNotificationSender(
        IApplicationDbContext db,
        IEmailSender email,
        IPushSender push,
        IDateTimeProvider clock,
        ILogger<ReaderNotificationSender> logger)
    {
        _db = db;
        _email = email;
        _push = push;
        _clock = clock;
        _logger = logger;
    }

    public string Channel => _push.IsConfigured ? "EMAIL+PUSH" : "EMAIL";

    public Task SendAsync(Guid readerId, string title, string body, string? link = null, CancellationToken ct = default) =>
        SendAsync(readerId, NotificationKinds.System, title, body, link, null, ct);

    public async Task SendAsync(
        Guid readerId,
        string kind,
        string title,
        string body,
        string? link,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            ReaderId = readerId,
            Type = kind,
            Title = title,
            Body = body,
            Link = link,
            CreatedAt = _clock.Now
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        var enabled = await Application.Features.Opac.NotificationPreferenceRules.IsEnabledAsync(_db, readerId, kind, ct);

        if (!enabled)
        {
            return;
        }

        var email = await _db.Readers
            .Where(r => r.Id == readerId)
            .Select(r => r.Email)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(email))
        {
            await _email.SendAsync(new EmailMessage(new[] { email }, title, body), ct);
        }

        if (!_push.IsConfigured)
        {
            return;
        }

        var tokens = await _db.DeviceTokens
            .Where(device => device.ReaderId == readerId && device.IsActive)
            .Select(device => device.Token)
            .ToListAsync(ct);

        if (tokens.Count == 0)
        {
            return;
        }

        var payload = new Dictionary<string, string>(data ?? new Dictionary<string, string>())
        {
            ["kind"] = kind,
            ["notificationId"] = notification.Id.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(link))
        {
            payload["link"] = link;
        }

        var result = await _push.SendAsync(tokens, title, body, payload, ct);

        if (result.DeadTokens.Count > 0)
        {
            var dead = await _db.DeviceTokens
                .Where(device => result.DeadTokens.Contains(device.Token))
                .ToListAsync(ct);

            foreach (var device in dead)
            {
                device.IsActive = false;
                device.LastSeenAt = _clock.Now;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Gỡ {Count} mã thiết bị không còn đăng ký của bạn đọc {ReaderId}", dead.Count, readerId);
        }
    }
}
