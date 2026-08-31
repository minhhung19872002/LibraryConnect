using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.Backups;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Jobs;

/// <summary>
/// Recurring housekeeping run by Hangfire (section 6.5): the scheduled backup, the audit retention
/// sweep and the expired-token cleanup.
///
/// Each method is the entry point of a Hangfire job, so it owns its own error handling: a failure is
/// logged and, where it matters to an administrator, emailed — it must never leave the recurring job
/// in a broken state.
/// </summary>
public class SystemMaintenanceJobs
{
    private readonly ISender _sender;
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IEmailSender _email;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SystemMaintenanceJobs> _logger;

    public SystemMaintenanceJobs(
        ISender sender,
        IApplicationDbContext db,
        ISystemParameterService parameters,
        IEmailSender email,
        IDateTimeProvider clock,
        ILogger<SystemMaintenanceJobs> logger)
    {
        _sender = sender;
        _db = db;
        _parameters = parameters;
        _email = email;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Sao lưu tự động theo lịch cấu hình (I.5).</summary>
    public async Task RunScheduledBackupAsync()
    {
        if (!await _parameters.GetAsync("BACKUP.AUTO_ENABLED", true))
        {
            _logger.LogInformation("Sao lưu tự động đang tắt, bỏ qua lần chạy này");
            return;
        }

        var includeFiles = await _parameters.GetAsync("BACKUP.INCLUDE_FILES", true);
        var result = await _sender.Send(new CreateBackupCommand(BackupType.Full, includeFiles, IsAuto: true));

        if (result.Status == BackupStatus.Success)
        {
            _logger.LogInformation("Sao lưu tự động thành công: {File}", result.FileName);
            return;
        }

        _logger.LogError("Sao lưu tự động thất bại: {Message}", result.Message);

        var notifyEmail = await _parameters.GetAsync("BACKUP.NOTIFY_EMAIL", string.Empty);
        if (!string.IsNullOrWhiteSpace(notifyEmail))
        {
            var libraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện");

            await _email.SendAsync(new EmailMessage(
                new[] { notifyEmail },
                $"[LibraryConnect] Sao lưu tự động thất bại — {libraryName}",
                $"""
                 <p>Tác vụ sao lưu tự động lúc {_clock.Now:HH:mm dd/MM/yyyy} đã <b>thất bại</b>.</p>
                 <p>Lý do: {result.Message}</p>
                 <p>Vui lòng kiểm tra dung lượng ổ đĩa và nhật ký hệ thống.</p>
                 """));
        }
    }

    /// <summary>
    /// Dọn nhật ký quá thời hạn lưu trữ (I.4).
    ///
    /// Only entities with an explicit retention setting are touched. The E-HSMT requires permanent
    /// retention by default, so a null <c>RetentionDays</c> means the rows are kept forever.
    /// </summary>
    public async Task PurgeExpiredAuditLogsAsync()
    {
        var settings = await _db.AuditSettings
            .Where(setting => setting.RetentionDays != null && setting.RetentionDays > 0)
            .Select(setting => new { setting.Entity, Days = setting.RetentionDays!.Value })
            .ToListAsync();

        if (settings.Count == 0)
        {
            return;
        }

        var removed = 0;

        foreach (var setting in settings)
        {
            var cutoff = _clock.Now.AddDays(-setting.Days);

            var expired = await _db.AuditLogs
                .Where(log => log.Entity == setting.Entity && log.OccurredAt < cutoff)
                .Take(10_000)
                .ToListAsync();

            if (expired.Count == 0)
            {
                continue;
            }

            _db.AuditLogs.RemoveRange(expired);
            removed += expired.Count;
        }

        if (removed > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Đã dọn {Count} bản ghi nhật ký quá hạn lưu trữ", removed);
        }
    }

    /// <summary>Xóa refresh token đã hết hạn hoặc bị thu hồi từ lâu.</summary>
    public async Task CleanupExpiredTokensAsync()
    {
        // Revoked tokens are kept for a month so a session-hijacking investigation still has a trail.
        var cutoff = _clock.Now.AddDays(-30);

        var expired = await _db.RefreshTokens
            .Where(token => token.ExpiresAt < cutoff || (token.RevokedAt != null && token.RevokedAt < cutoff))
            .Take(10_000)
            .ToListAsync();

        if (expired.Count == 0)
        {
            return;
        }

        _db.RefreshTokens.RemoveRange(expired);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Đã dọn {Count} refresh token hết hạn", expired.Count);
    }
}
