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
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogInformation(
                "Email sending is disabled; message \"{Subject}\" to {Recipients} was not delivered",
                message.Subject, string.Join(", ", message.To));
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

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

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port,
            _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        }

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            return false;
        }

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port,
                _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            }

            await client.DisconnectAsync(true, ct);
            return true;
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or AuthenticationException or IOException)
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
