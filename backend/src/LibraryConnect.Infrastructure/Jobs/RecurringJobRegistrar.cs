using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Digital;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Jobs;

/// <summary>
/// Registers the recurring jobs once the host is up, reading their schedules from the system
/// parameters so an administrator can change the backup time from the UI without a redeploy.
///
/// Runs as a hosted service rather than inline in Program.cs because the schedule comes from the
/// database, which is only reachable after migrations have completed.
/// </summary>
public class RecurringJobRegistrar : IHostedService
{
    public const string BackupJobId = "libraryconnect:auto-backup";
    public const string AuditPurgeJobId = "libraryconnect:audit-purge";
    public const string TokenCleanupJobId = "libraryconnect:token-cleanup";
    public const string OverdueJobId = "libraryconnect:circulation-overdue";
    public const string DueSoonJobId = "libraryconnect:circulation-due-soon";
    public const string HoldExpiryJobId = "libraryconnect:circulation-hold-expiry";
    public const string DigitalExpiryJobId = "libraryconnect:digital-access-expiry";
    public const string DigitalUploadCleanupJobId = "libraryconnect:digital-upload-cleanup";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringJobRegistrar> _logger;

    public RecurringJobRegistrar(IServiceScopeFactory scopeFactory, ILogger<RecurringJobRegistrar> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
            var parameters = scope.ServiceProvider.GetRequiredService<ISystemParameterService>();

            var backupCron = await parameters.GetAsync("BACKUP.SCHEDULE_CRON", "0 2 * * *", cancellationToken);
            var autoEnabled = await parameters.GetAsync("BACKUP.AUTO_ENABLED", true, cancellationToken);

            if (autoEnabled)
            {
                jobs.AddOrUpdateRecurring<SystemMaintenanceJobs>(
                    BackupJobId, job => job.RunScheduledBackupAsync(), backupCron);
                _logger.LogInformation("Đã đăng ký sao lưu tự động theo lịch '{Cron}'", backupCron);
            }
            else
            {
                jobs.RemoveRecurring(BackupJobId);
            }

            // Housekeeping runs in the small hours, after the backup has finished.
            jobs.AddOrUpdateRecurring<SystemMaintenanceJobs>(
                AuditPurgeJobId, job => job.PurgeExpiredAuditLogsAsync(), "30 3 * * *");

            jobs.AddOrUpdateRecurring<SystemMaintenanceJobs>(
                TokenCleanupJobId, job => job.CleanupExpiredTokensAsync(), "0 4 * * *");

            // Lưu thông chạy sớm hơn giờ mở cửa: cán bộ đến quầy là đã thấy đúng trạng thái quá hạn
            // của hôm nay, và bạn đọc nhận thư nhắc từ sáng.
            jobs.AddOrUpdateRecurring<ICirculationDailyJobs>(
                OverdueJobId, job => job.MarkOverdueAsync(), "5 0 * * *");

            jobs.AddOrUpdateRecurring<ICirculationDailyJobs>(
                DueSoonJobId, job => job.SendDueSoonRemindersAsync(), "0 7 * * *");

            jobs.AddOrUpdateRecurring<ICirculationDailyJobs>(
                HoldExpiryJobId, job => job.ExpireHoldsAsync(), "30 0 * * *");

            // Tài liệu số: quyền đọc hết hạn được đóng lại từ nửa đêm, còn các phiên tải dở dang
            // dọn vào lúc rảnh nhất trong ngày vì việc này đụng tới kho đối tượng.
            jobs.AddOrUpdateRecurring<IDigitalMaintenanceJob>(
                DigitalExpiryJobId, job => job.ExpireAccessRequestsAsync(CancellationToken.None), "10 0 * * *");

            jobs.AddOrUpdateRecurring<IDigitalMaintenanceJob>(
                DigitalUploadCleanupJobId,
                job => job.CleanUploadSessionsAsync(CancellationToken.None),
                "45 3 * * *");
        }
        catch (Exception ex)
        {
            // A scheduling problem must not stop the library system from serving readers.
            _logger.LogError(ex, "Không đăng ký được các tác vụ nền định kỳ");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
