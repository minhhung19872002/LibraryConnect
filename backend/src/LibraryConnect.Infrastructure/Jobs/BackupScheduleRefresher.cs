using LibraryConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Jobs;

/// <summary>
/// Đặt lại việc sao lưu định kỳ theo tham số hiện tại (I.5).
///
/// Dùng ở hai chỗ: lúc máy chủ khởi động, và ngay sau khi người quản trị lưu tham số sao lưu. Trước
/// 04/09/2026 chỉ có chỗ thứ nhất, nên đổi giờ sao lưu trên giao diện không có tác dụng cho tới lần
/// khởi động lại — mà màn hình vẫn hiện giờ mới, không ai biết là nó chưa chạy theo giờ ấy.
/// </summary>
public class BackupScheduleRefresher : IBackupScheduleRefresher
{
    private readonly IBackgroundJobService _jobs;
    private readonly ISystemParameterService _parameters;
    private readonly ILogger<BackupScheduleRefresher> _logger;

    public BackupScheduleRefresher(
        IBackgroundJobService jobs,
        ISystemParameterService parameters,
        ILogger<BackupScheduleRefresher> logger)
    {
        _jobs = jobs;
        _parameters = parameters;
        _logger = logger;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var cron = await _parameters.GetAsync("BACKUP.SCHEDULE_CRON", "0 2 * * *", ct);
        var enabled = await _parameters.GetAsync("BACKUP.AUTO_ENABLED", true, ct);

        if (!enabled)
        {
            _jobs.RemoveRecurring(RecurringJobRegistrar.BackupJobId);
            _logger.LogInformation("Sao lưu tự động đang tắt; đã gỡ việc định kỳ");
            return;
        }

        _jobs.AddOrUpdateRecurring<SystemMaintenanceJobs>(
            RecurringJobRegistrar.BackupJobId, job => job.RunScheduledBackupAsync(), cron);

        _logger.LogInformation("Sao lưu tự động chạy theo lịch '{Cron}'", cron);
    }

    public string? CurrentCron() => _jobs.GetRecurringCron(RecurringJobRegistrar.BackupJobId);
}
