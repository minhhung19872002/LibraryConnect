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
/// Default notification channel: writes an in-app notification and, when the reader has an address,
/// sends an email. The FCM channel of the mobile release plugs in behind the same interface.
/// </summary>
public class EmailNotificationSender : INotificationSender
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IDateTimeProvider _clock;

    public EmailNotificationSender(IApplicationDbContext db, IEmailSender email, IDateTimeProvider clock)
    {
        _db = db;
        _email = email;
        _clock = clock;
    }

    public string Channel => "EMAIL";

    public async Task SendAsync(Guid readerId, string title, string body, string? link = null, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            ReaderId = readerId,
            Type = "SYSTEM",
            Title = title,
            Body = body,
            Link = link,
            CreatedAt = _clock.Now
        });

        await _db.SaveChangesAsync(ct);

        var email = await _db.Readers
            .Where(r => r.Id == readerId)
            .Select(r => r.Email)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(email))
        {
            await _email.SendAsync(new EmailMessage(new[] { email }, title, body), ct);
        }
    }
}
